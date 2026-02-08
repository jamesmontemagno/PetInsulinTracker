using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PetInsulinTracker.Api.Models;
using PetInsulinTracker.Api.Services;
using PetInsulinTracker.Shared.DTOs;

namespace PetInsulinTracker.Api.Functions;

public class SyncFunctions
{
	private readonly ILogger<SyncFunctions> _logger;
	private readonly TableStorageService _storage;

	public SyncFunctions(ILogger<SyncFunctions> logger, TableStorageService storage)
	{
		_logger = logger;
		_storage = storage;
	}

	private static DateTimeOffset ClampLastModified(DateTimeOffset d)
	{
		var min = new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);
		return d < min ? DateTimeOffset.UtcNow : d;
	}

	/// <summary>
	/// Azure Table Storage minimum supported DateTime is 1601-01-01.
	/// </summary>
	private static readonly DateTimeOffset AzureTableMinDate = new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	/// Azure Table Storage requires DateTime values to be UTC.
	/// Preserves the numeric value by re-specifying the Kind as UTC.
	/// </summary>
	private static DateTime EnsureUtc(DateTime dt) =>
		DateTime.SpecifyKind(dt, DateTimeKind.Utc);

	private static DateTime? EnsureUtc(DateTime? dt) =>
		dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

	[Function("Sync")]
	public async Task<HttpResponseData> Sync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync")] HttpRequestData req)
	{
		SyncRequest? syncRequest;
		try
		{
			syncRequest = await req.ReadFromJsonAsync<SyncRequest>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize sync request body");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (syncRequest is null || string.IsNullOrEmpty(syncRequest.PetId) || string.IsNullOrEmpty(syncRequest.DeviceUserId))
		{
			_logger.LogWarning("Sync request rejected: missing PetId or DeviceUserId");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var petId = syncRequest.PetId;
		var deviceUserId = syncRequest.DeviceUserId;
		_logger.LogInformation(
			"Sync request for pet {PetId} from {DeviceUserId} — LastSync={LastSync}, Pets={PetCount}, InsulinLogs={InsulinCount}, FeedingLogs={FeedingCount}, WeightLogs={WeightCount}, VetInfos={VetCount}, Schedules={ScheduleCount}",
			petId, deviceUserId, syncRequest.LastSyncTimestamp,
			syncRequest.Pets.Count, syncRequest.InsulinLogs.Count, syncRequest.FeedingLogs.Count,
			syncRequest.WeightLogs.Count, syncRequest.VetInfos.Count, syncRequest.Schedules.Count);

		try
		{
		// Determine access level
		_logger.LogDebug("Looking up pet {PetId} in storage", petId);
		var pet = await _storage.GetPetAsync(petId);
		string accessLevel;

		if (pet is null)
		{
			_logger.LogInformation("Pet {PetId} not found on server, checking sync payload for creation data", petId);
			// Pet doesn't exist on server yet — create from sync data if provided
			var petData = syncRequest.Pets.FirstOrDefault(p => p.Id == petId);
			if (petData is null)
			{
				_logger.LogWarning("Pet {PetId} not found on server and not included in sync payload — returning 404", petId);
				return req.CreateResponse(HttpStatusCode.NotFound);
			}

			// The sender becomes the owner
			await _storage.UpsertPetAsync(new PetEntity
			{
				RowKey = petId,
				OwnerId = deviceUserId,
				OwnerName = petData.OwnerName,
				AccessLevel = "owner",
				Name = petData.Name,
				Species = petData.Species,
				Breed = petData.Breed,
				DateOfBirth = EnsureUtc(petData.DateOfBirth),
				InsulinType = petData.InsulinType,
				InsulinConcentration = petData.InsulinConcentration,
				CurrentDoseIU = petData.CurrentDoseIU,
				WeightUnit = petData.WeightUnit,
				CurrentWeight = petData.CurrentWeight,
				DefaultFoodName = petData.DefaultFoodName,
				DefaultFoodAmount = petData.DefaultFoodAmount,
				DefaultFoodUnit = petData.DefaultFoodUnit,
				DefaultFoodType = petData.DefaultFoodType,
				LastModified = ClampLastModified(petData.LastModified),
				IsDeleted = petData.IsDeleted
			});
			pet = await _storage.GetPetAsync(petId);
			accessLevel = "owner";
			_logger.LogInformation("Auto-created pet {PetId} for owner {OwnerId} during sync", petId, deviceUserId);
		}
		else if (pet.OwnerId == deviceUserId)
		{
			accessLevel = "owner";
			_logger.LogDebug("User {DeviceUserId} is owner of pet {PetId}", deviceUserId, petId);
		}
		else
		{
			// Single point read: PK=petId, RK=deviceUserId
			_logger.LogDebug("User {DeviceUserId} is not owner (owner={OwnerId}), checking redemption", deviceUserId, pet.OwnerId);
			var redemption = await _storage.GetRedemptionAsync(petId, deviceUserId);

			if (redemption is null)
			{
				_logger.LogWarning("Unauthorized user {DeviceUserId} attempted sync for pet {PetId}", deviceUserId, petId);
				return req.CreateResponse(HttpStatusCode.Forbidden);
			}

			if (redemption.IsRevoked)
			{
				_logger.LogWarning("Revoked user {DeviceUserId} attempted sync for pet {PetId}", deviceUserId, petId);
				return req.CreateResponse(HttpStatusCode.Forbidden);
			}

			accessLevel = redemption.AccessLevel;
			_logger.LogDebug("User {DeviceUserId} has access level {AccessLevel} for pet {PetId}", deviceUserId, accessLevel, petId);
		}

		_logger.LogInformation("Uploading client changes for pet {PetId} with access level {AccessLevel}", petId, accessLevel);

		// Upload client changes based on access level
		if (accessLevel == "owner")
		{
			// Owner can upload everything
			_logger.LogDebug("Upserting {Count} pets, {VetCount} vet infos, {ScheduleCount} schedules",
				syncRequest.Pets.Count, syncRequest.VetInfos.Count, syncRequest.Schedules.Count);
			foreach (var p in syncRequest.Pets)
			{
				await _storage.UpsertPetAsync(new PetEntity
				{
					RowKey = p.Id,
					OwnerId = deviceUserId,
					OwnerName = p.OwnerName,
					AccessLevel = p.AccessLevel,
					Name = p.Name,
					Species = p.Species,
					Breed = p.Breed,
					DateOfBirth = EnsureUtc(p.DateOfBirth),
					InsulinType = p.InsulinType,
					InsulinConcentration = p.InsulinConcentration,
					CurrentDoseIU = p.CurrentDoseIU,
					WeightUnit = p.WeightUnit,
					CurrentWeight = p.CurrentWeight,
					DefaultFoodName = p.DefaultFoodName,
					DefaultFoodAmount = p.DefaultFoodAmount,
					DefaultFoodUnit = p.DefaultFoodUnit,
					DefaultFoodType = p.DefaultFoodType,
				LastModified = ClampLastModified(p.LastModified),
					IsDeleted = p.IsDeleted
				});
			}

			foreach (var info in syncRequest.VetInfos)
			{
				await _storage.UpsertEntityAsync("VetInfos", new VetInfoEntity
				{
					PartitionKey = petId, RowKey = info.Id, PetId = info.PetId,
					VetName = info.VetName, ClinicName = info.ClinicName,
					Phone = info.Phone, EmergencyPhone = info.EmergencyPhone,
					Address = info.Address, Email = info.Email, Notes = info.Notes,
				LastModified = ClampLastModified(info.LastModified), IsDeleted = info.IsDeleted
				});
			}

		}

		if (accessLevel is "owner" or "full")
		{
			foreach (var s in syncRequest.Schedules)
			{
				await _storage.UpsertEntityAsync("Schedules", new ScheduleEntity
				{
					PartitionKey = petId, RowKey = s.Id, PetId = s.PetId,
					ScheduleType = s.ScheduleType, Label = s.Label,
					TimeTicks = s.TimeTicks, IsEnabled = s.IsEnabled,
					ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = ClampLastModified(s.LastModified), IsDeleted = s.IsDeleted
				});
			}
		}

		// Owner and full can upload all log types; guest can only upload insulin and feeding logs
		if (accessLevel is "owner" or "full")
		{
			foreach (var log in syncRequest.WeightLogs)
			{
				await _storage.UpsertEntityAsync("WeightLogs", new WeightLogEntity
				{
					PartitionKey = petId, RowKey = log.Id, PetId = log.PetId,
					Weight = log.Weight, WeightUnit = log.Unit, RecordedAt = EnsureUtc(log.RecordedAt),
					Notes = log.Notes, LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
				LastModified = ClampLastModified(log.LastModified), IsDeleted = log.IsDeleted
				});
			}
		}

		// All access levels can upload insulin and feeding logs
		_logger.LogDebug("Upserting {InsulinCount} insulin logs and {FeedingCount} feeding logs",
			syncRequest.InsulinLogs.Count, syncRequest.FeedingLogs.Count);
		foreach (var log in syncRequest.InsulinLogs)
		{
			await _storage.UpsertEntityAsync("InsulinLogs", new InsulinLogEntity
			{
				PartitionKey = petId, RowKey = log.Id, PetId = log.PetId,
				DoseIU = log.DoseIU, AdministeredAt = EnsureUtc(log.AdministeredAt),
				InjectionSite = log.InjectionSite, Notes = log.Notes,
				LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
				LastModified = ClampLastModified(log.LastModified), IsDeleted = log.IsDeleted
			});
		}

		foreach (var log in syncRequest.FeedingLogs)
		{
			await _storage.UpsertEntityAsync("FeedingLogs", new FeedingLogEntity
			{
				PartitionKey = petId, RowKey = log.Id, PetId = log.PetId,
				FoodName = log.FoodName, Amount = log.Amount, Unit = log.Unit,
				FoodType = log.FoodType, FedAt = EnsureUtc(log.FedAt), Notes = log.Notes,
				LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
				LastModified = ClampLastModified(log.LastModified), IsDeleted = log.IsDeleted
			});
		}

		// Download server changes since last sync
		_logger.LogInformation("Upload complete for pet {PetId}, downloading server changes since {Since}", petId, syncRequest.LastSyncTimestamp);
		var since = syncRequest.LastSyncTimestamp < AzureTableMinDate ? AzureTableMinDate : syncRequest.LastSyncTimestamp;
		var now = DateTimeOffset.UtcNow;

		var serverPets = await _storage.GetPetsModifiedSinceAsync(petId, since);
		var serverInsulinLogs = await _storage.GetEntitiesModifiedSinceAsync<InsulinLogEntity>("InsulinLogs", petId, since);
		var serverFeedingLogs = await _storage.GetEntitiesModifiedSinceAsync<FeedingLogEntity>("FeedingLogs", petId, since);

		// Guest only sees their own logs
		if (accessLevel == "guest")
		{
			serverInsulinLogs = serverInsulinLogs.Where(l => l.LoggedById == deviceUserId).ToList();
			serverFeedingLogs = serverFeedingLogs.Where(l => l.LoggedById == deviceUserId).ToList();
		}

		// Weight logs: owner and full only
		var serverWeightLogs = accessLevel is "owner" or "full"
			? await _storage.GetEntitiesModifiedSinceAsync<WeightLogEntity>("WeightLogs", petId, since)
			: [];

		// Vet info and schedules: all can see, only owner can modify vet info; owner/full can modify schedules
		var serverVetInfos = accessLevel is "owner" or "full"
			? await _storage.GetEntitiesModifiedSinceAsync<VetInfoEntity>("VetInfos", petId, since)
			: [];

		var serverSchedules = await _storage.GetEntitiesModifiedSinceAsync<ScheduleEntity>("Schedules", petId, since);

		var syncResponse = new SyncResponse
		{
			SyncTimestamp = now,
			Pets = serverPets.Select(p => new PetDto
			{
				Id = p.RowKey, OwnerId = p.OwnerId, OwnerName = p.OwnerName,
				AccessLevel = p.AccessLevel, Name = p.Name, Species = p.Species,
				Breed = p.Breed, DateOfBirth = p.DateOfBirth,
				InsulinType = p.InsulinType, InsulinConcentration = p.InsulinConcentration,
				CurrentDoseIU = p.CurrentDoseIU, WeightUnit = p.WeightUnit,
				CurrentWeight = p.CurrentWeight,
				DefaultFoodName = p.DefaultFoodName,
				DefaultFoodAmount = p.DefaultFoodAmount,
				DefaultFoodUnit = p.DefaultFoodUnit,
				DefaultFoodType = p.DefaultFoodType,
				LastModified = p.LastModified, IsDeleted = p.IsDeleted
			}).ToList(),
			InsulinLogs = serverInsulinLogs.Select(l => new InsulinLogDto
			{
				Id = l.RowKey, PetId = l.PetId, DoseIU = l.DoseIU,
				AdministeredAt = l.AdministeredAt, InjectionSite = l.InjectionSite,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById,
				LastModified = l.LastModified, IsDeleted = l.IsDeleted
			}).ToList(),
			FeedingLogs = serverFeedingLogs.Select(l => new FeedingLogDto
			{
				Id = l.RowKey, PetId = l.PetId, FoodName = l.FoodName,
				Amount = l.Amount, Unit = l.Unit, FoodType = l.FoodType,
				FedAt = l.FedAt, Notes = l.Notes, LoggedBy = l.LoggedBy,
				LoggedById = l.LoggedById, LastModified = l.LastModified,
				IsDeleted = l.IsDeleted
			}).ToList(),
			WeightLogs = serverWeightLogs.Select(l => new WeightLogDto
			{
				Id = l.RowKey, PetId = l.PetId, Weight = l.Weight,
				Unit = l.WeightUnit, RecordedAt = l.RecordedAt,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById,
				LastModified = l.LastModified, IsDeleted = l.IsDeleted
			}).ToList(),
			VetInfos = serverVetInfos.Select(v => new VetInfoDto
			{
				Id = v.RowKey, PetId = v.PetId, VetName = v.VetName,
				ClinicName = v.ClinicName, Phone = v.Phone,
				EmergencyPhone = v.EmergencyPhone, Address = v.Address,
				Email = v.Email, Notes = v.Notes,
				LastModified = v.LastModified, IsDeleted = v.IsDeleted
			}).ToList(),
			Schedules = serverSchedules.Select(s => new ScheduleDto
			{
				Id = s.RowKey, PetId = s.PetId, ScheduleType = s.ScheduleType,
				Label = s.Label, TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified, IsDeleted = s.IsDeleted
			}).ToList()
		};

		_logger.LogInformation(
			"Sync complete for pet {PetId} — returning Pets={PetCount}, InsulinLogs={InsulinCount}, FeedingLogs={FeedingCount}, WeightLogs={WeightCount}, VetInfos={VetCount}, Schedules={ScheduleCount}",
			petId, syncResponse.Pets.Count, syncResponse.InsulinLogs.Count,
			syncResponse.FeedingLogs.Count, syncResponse.WeightLogs.Count,
			syncResponse.VetInfos.Count, syncResponse.Schedules.Count);

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(syncResponse);
		return response;

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Sync failed for pet {PetId} from {DeviceUserId}. Exception: {ExMessage}", petId, deviceUserId, ex.Message);
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteAsJsonAsync(new { error = ex.Message, stackTrace = ex.StackTrace });
			return errorResponse;
		}
	}
}
