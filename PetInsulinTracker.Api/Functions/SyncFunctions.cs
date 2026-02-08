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

	[Function("Sync")]
	public async Task<HttpResponseData> Sync(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sync")] HttpRequestData req)
	{
		var syncRequest = await req.ReadFromJsonAsync<SyncRequest>();
		if (syncRequest is null || string.IsNullOrEmpty(syncRequest.PetId) || string.IsNullOrEmpty(syncRequest.DeviceUserId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var petId = syncRequest.PetId;
		var deviceUserId = syncRequest.DeviceUserId;
		_logger.LogInformation("Sync request for pet {PetId} from {DeviceUserId}", petId, deviceUserId);

		// Determine access level
		var pet = await _storage.GetPetAsync(petId);
		if (pet is null)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		string accessLevel;
		if (pet.OwnerId == deviceUserId)
		{
			accessLevel = "owner";
		}
		else
		{
			// Look up redemption across all share codes for this pet
			var shareCodes = await _storage.GetShareCodesByPetIdAsync(petId);
			ShareRedemptionEntity? redemption = null;
			foreach (var sc in shareCodes)
			{
				redemption = await _storage.GetRedemptionAsync(sc.RowKey, deviceUserId);
				if (redemption is not null) break;
			}

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
		}

		// Upload client changes based on access level
		if (accessLevel == "owner")
		{
			// Owner can upload everything
			foreach (var p in syncRequest.Pets)
			{
				await _storage.UpsertPetAsync(new PetEntity
				{
					RowKey = p.Id,
					OwnerId = p.OwnerId,
					OwnerName = p.OwnerName,
					AccessLevel = p.AccessLevel,
					Name = p.Name,
					Species = p.Species,
					Breed = p.Breed,
					DateOfBirth = p.DateOfBirth,
					InsulinType = p.InsulinType,
					InsulinConcentration = p.InsulinConcentration,
					CurrentDoseIU = p.CurrentDoseIU,
					WeightUnit = p.WeightUnit,
					CurrentWeight = p.CurrentWeight,
					LastModified = p.LastModified,
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
					LastModified = info.LastModified, IsDeleted = info.IsDeleted
				});
			}

			foreach (var s in syncRequest.Schedules)
			{
				await _storage.UpsertEntityAsync("Schedules", new ScheduleEntity
				{
					PartitionKey = petId, RowKey = s.Id, PetId = s.PetId,
					ScheduleType = s.ScheduleType, Label = s.Label,
					TimeTicks = s.TimeTicks, IsEnabled = s.IsEnabled,
					ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
					LastModified = s.LastModified, IsDeleted = s.IsDeleted
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
					Weight = log.Weight, WeightUnit = log.Unit, RecordedAt = log.RecordedAt,
					Notes = log.Notes, LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
					LastModified = log.LastModified, IsDeleted = log.IsDeleted
				});
			}
		}

		// All access levels can upload insulin and feeding logs
		foreach (var log in syncRequest.InsulinLogs)
		{
			await _storage.UpsertEntityAsync("InsulinLogs", new InsulinLogEntity
			{
				PartitionKey = petId, RowKey = log.Id, PetId = log.PetId,
				DoseIU = log.DoseIU, AdministeredAt = log.AdministeredAt,
				InjectionSite = log.InjectionSite, Notes = log.Notes,
				LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
				LastModified = log.LastModified, IsDeleted = log.IsDeleted
			});
		}

		foreach (var log in syncRequest.FeedingLogs)
		{
			await _storage.UpsertEntityAsync("FeedingLogs", new FeedingLogEntity
			{
				PartitionKey = petId, RowKey = log.Id, PetId = log.PetId,
				FoodName = log.FoodName, Amount = log.Amount, Unit = log.Unit,
				FoodType = log.FoodType, FedAt = log.FedAt, Notes = log.Notes,
				LoggedBy = log.LoggedBy, LoggedById = log.LoggedById,
				LastModified = log.LastModified, IsDeleted = log.IsDeleted
			});
		}

		// Download server changes since last sync
		var since = syncRequest.LastSyncTimestamp;
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

		// Vet info and schedules: all can see, only owner can modify (handled above in upload)
		var serverVetInfos = accessLevel is "owner" or "full"
			? await _storage.GetEntitiesModifiedSinceAsync<VetInfoEntity>("VetInfos", petId, since)
			: [];

		var serverSchedules = accessLevel == "owner"
			? await _storage.GetEntitiesModifiedSinceAsync<ScheduleEntity>("Schedules", petId, since)
			: [];

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

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(syncResponse);
		return response;
	}
}
