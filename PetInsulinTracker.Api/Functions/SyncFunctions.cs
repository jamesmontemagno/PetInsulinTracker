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
		if (syncRequest is null || string.IsNullOrEmpty(syncRequest.ShareCode))
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			return badResponse;
		}

		var shareCode = syncRequest.ShareCode;
		_logger.LogInformation("Sync request for share code {ShareCode}", shareCode);

		// Upload client changes
		foreach (var pet in syncRequest.Pets)
		{
			await _storage.UpsertPetAsync(shareCode, new PetEntity
			{
				RowKey = pet.Id,
				OwnerId = pet.OwnerId,
				AccessLevel = pet.AccessLevel,
				Name = pet.Name,
				Species = pet.Species,
				Breed = pet.Breed,
				DateOfBirth = pet.DateOfBirth,
				InsulinType = pet.InsulinType,
				InsulinConcentration = pet.InsulinConcentration,
				CurrentDoseIU = pet.CurrentDoseIU,
				WeightUnit = pet.WeightUnit,
				CurrentWeight = pet.CurrentWeight,
				ShareCode = shareCode,
				LastModified = pet.LastModified
			});
		}

		foreach (var log in syncRequest.InsulinLogs)
		{
			await _storage.UpsertEntityAsync("InsulinLogs", new InsulinLogEntity
			{
				PartitionKey = shareCode,
				RowKey = log.Id,
				PetId = log.PetId,
				DoseIU = log.DoseIU,
				AdministeredAt = log.AdministeredAt,
				InjectionSite = log.InjectionSite,
				Notes = log.Notes,
				LoggedBy = log.LoggedBy,
				LoggedById = log.LoggedById,
				LastModified = log.LastModified
			});
		}

		foreach (var log in syncRequest.FeedingLogs)
		{
			await _storage.UpsertEntityAsync("FeedingLogs", new FeedingLogEntity
			{
				PartitionKey = shareCode,
				RowKey = log.Id,
				PetId = log.PetId,
				FoodName = log.FoodName,
				Amount = log.Amount,
				Unit = log.Unit,
				FoodType = log.FoodType,
				FedAt = log.FedAt,
				Notes = log.Notes,
				LoggedBy = log.LoggedBy,
				LoggedById = log.LoggedById,
				LastModified = log.LastModified
			});
		}

		foreach (var log in syncRequest.WeightLogs)
		{
			await _storage.UpsertEntityAsync("WeightLogs", new WeightLogEntity
			{
				PartitionKey = shareCode,
				RowKey = log.Id,
				PetId = log.PetId,
				Weight = log.Weight,
				WeightUnit = log.Unit,
				RecordedAt = log.RecordedAt,
				Notes = log.Notes,
				LoggedBy = log.LoggedBy,
				LoggedById = log.LoggedById,
				LastModified = log.LastModified
			});
		}

		foreach (var info in syncRequest.VetInfos)
		{
			await _storage.UpsertEntityAsync("VetInfos", new VetInfoEntity
			{
				PartitionKey = shareCode,
				RowKey = info.Id,
				PetId = info.PetId,
				VetName = info.VetName,
				ClinicName = info.ClinicName,
				Phone = info.Phone,
				EmergencyPhone = info.EmergencyPhone,
				Address = info.Address,
				Email = info.Email,
				Notes = info.Notes,
				LastModified = info.LastModified
			});
		}

		foreach (var s in syncRequest.Schedules)
		{
			await _storage.UpsertEntityAsync("Schedules", new ScheduleEntity
			{
				PartitionKey = shareCode,
				RowKey = s.Id,
				PetId = s.PetId,
				ScheduleType = s.ScheduleType,
				Label = s.Label,
				TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified
			});
		}

		// Download server changes since last sync
		var since = syncRequest.LastSyncTimestamp;
		var now = DateTimeOffset.UtcNow;

		var serverPets = await _storage.GetPetsByShareCodeAsync(shareCode);
		var serverInsulinLogs = await _storage.GetEntitiesByPartitionAsync<InsulinLogEntity>("InsulinLogs", shareCode);
		var serverFeedingLogs = await _storage.GetEntitiesByPartitionAsync<FeedingLogEntity>("FeedingLogs", shareCode);
		var serverWeightLogs = await _storage.GetEntitiesByPartitionAsync<WeightLogEntity>("WeightLogs", shareCode);
		var serverVetInfos = await _storage.GetEntitiesByPartitionAsync<VetInfoEntity>("VetInfos", shareCode);
		var serverSchedules = await _storage.GetEntitiesByPartitionAsync<ScheduleEntity>("Schedules", shareCode);

		var syncResponse = new SyncResponse
		{
			SyncTimestamp = now,
			Pets = serverPets.Select(p => new PetDto
			{
				Id = p.RowKey,
				OwnerId = p.OwnerId,
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
				ShareCode = p.ShareCode,
				LastModified = p.LastModified
			}).ToList(),
			InsulinLogs = serverInsulinLogs.Select(l => new InsulinLogDto
			{
				Id = l.RowKey,
				PetId = l.PetId,
				DoseIU = l.DoseIU,
				AdministeredAt = l.AdministeredAt,
				InjectionSite = l.InjectionSite,
				Notes = l.Notes,
				LoggedBy = l.LoggedBy,
				LoggedById = l.LoggedById,
				LastModified = l.LastModified
			}).ToList(),
			FeedingLogs = serverFeedingLogs.Select(l => new FeedingLogDto
			{
				Id = l.RowKey,
				PetId = l.PetId,
				FoodName = l.FoodName,
				Amount = l.Amount,
				Unit = l.Unit,
				FoodType = l.FoodType,
				FedAt = l.FedAt,
				Notes = l.Notes,
				LoggedBy = l.LoggedBy,
				LoggedById = l.LoggedById,
				LastModified = l.LastModified
			}).ToList(),
			WeightLogs = serverWeightLogs.Select(l => new WeightLogDto
			{
				Id = l.RowKey,
				PetId = l.PetId,
				Weight = l.Weight,
				Unit = l.WeightUnit,
				RecordedAt = l.RecordedAt,
				Notes = l.Notes,
				LoggedBy = l.LoggedBy,
				LoggedById = l.LoggedById,
				LastModified = l.LastModified
			}).ToList(),
			VetInfos = serverVetInfos.Select(v => new VetInfoDto
			{
				Id = v.RowKey,
				PetId = v.PetId,
				VetName = v.VetName,
				ClinicName = v.ClinicName,
				Phone = v.Phone,
				EmergencyPhone = v.EmergencyPhone,
				Address = v.Address,
				Email = v.Email,
				Notes = v.Notes,
				LastModified = v.LastModified
			}).ToList(),
			Schedules = serverSchedules.Select(s => new ScheduleDto
			{
				Id = s.RowKey,
				PetId = s.PetId,
				ScheduleType = s.ScheduleType,
				Label = s.Label,
				TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified
			}).ToList()
		};

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(syncResponse);
		return response;
	}
}
