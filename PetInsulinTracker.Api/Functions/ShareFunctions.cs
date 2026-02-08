using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PetInsulinTracker.Api.Services;
using PetInsulinTracker.Shared.DTOs;

namespace PetInsulinTracker.Api.Functions;

public class ShareFunctions
{
	private readonly ILogger<ShareFunctions> _logger;
	private readonly TableStorageService _storage;
	private static readonly char[] ShareCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

	public ShareFunctions(ILogger<ShareFunctions> logger, TableStorageService storage)
	{
		_logger = logger;
		_storage = storage;
	}

	[Function("GenerateShareCode")]
	public async Task<HttpResponseData> GenerateShareCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "share/generate")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<ShareCodeRequest>();
		if (request is null || string.IsNullOrEmpty(request.PetId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		// Generate a unique 6-character code (no ambiguous chars)
		string code;
		do
		{
			code = GenerateCode(6);
		}
		while (await _storage.GetShareCodeAsync(code) is not null);

		await _storage.CreateShareCodeAsync(code, request.PetId, request.AccessLevel, request.OwnerId);
		_logger.LogInformation("Generated share code {Code} for pet {PetId}", code, request.PetId);

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(new ShareCodeResponse { ShareCode = code, AccessLevel = request.AccessLevel });
		return response;
	}

	[Function("RedeemShareCode")]
	public async Task<HttpResponseData> RedeemShareCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "share/redeem")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<RedeemShareCodeRequest>();
		if (request is null || string.IsNullOrEmpty(request.ShareCode))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var code = request.ShareCode;
		var shareCode = await _storage.GetShareCodeAsync(code);
		if (shareCode is null)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		var accessLevel = shareCode.AccessLevel ?? "full";

		// Record who redeemed
		if (!string.IsNullOrEmpty(request.DeviceUserId))
		{
			await _storage.CreateRedemptionAsync(code, request.DeviceUserId, request.DisplayName, accessLevel);
		}

		// Get all data for this share code
		var pets = await _storage.GetPetsByShareCodeAsync(code);
		var pet = pets.FirstOrDefault(p => !p.IsDeleted);
		if (pet is null)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		var insulinLogs = await _storage.GetEntitiesByPartitionAsync<Models.InsulinLogEntity>("InsulinLogs", code);
		var feedingLogs = await _storage.GetEntitiesByPartitionAsync<Models.FeedingLogEntity>("FeedingLogs", code);
		var weightLogs = await _storage.GetEntitiesByPartitionAsync<Models.WeightLogEntity>("WeightLogs", code);
		var vetInfos = await _storage.GetEntitiesByPartitionAsync<Models.VetInfoEntity>("VetInfos", code);
		var schedules = await _storage.GetEntitiesByPartitionAsync<Models.ScheduleEntity>("Schedules", code);

		var result = new RedeemShareCodeResponse
		{
			Pet = new PetDto
			{
				Id = pet.RowKey,
				OwnerId = pet.OwnerId,
				AccessLevel = accessLevel,
				Name = pet.Name,
				Species = pet.Species,
				Breed = pet.Breed,
				DateOfBirth = pet.DateOfBirth,
				InsulinType = pet.InsulinType,
				InsulinConcentration = pet.InsulinConcentration,
				CurrentDoseIU = pet.CurrentDoseIU,
				WeightUnit = pet.WeightUnit,
				CurrentWeight = pet.CurrentWeight,
				ShareCode = code,
				LastModified = pet.LastModified
			},
			InsulinLogs = accessLevel == "guest" ? [] : insulinLogs.Where(l => !l.IsDeleted).Select(l => new InsulinLogDto
			{
				Id = l.RowKey, PetId = l.PetId, DoseIU = l.DoseIU,
				AdministeredAt = l.AdministeredAt, InjectionSite = l.InjectionSite,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified
			}).ToList(),
			FeedingLogs = accessLevel == "guest" ? [] : feedingLogs.Where(l => !l.IsDeleted).Select(l => new FeedingLogDto
			{
				Id = l.RowKey, PetId = l.PetId, FoodName = l.FoodName,
				Amount = l.Amount, Unit = l.Unit, FoodType = l.FoodType,
				FedAt = l.FedAt, Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified
			}).ToList(),
			WeightLogs = accessLevel == "guest" ? [] : weightLogs.Where(l => !l.IsDeleted).Select(l => new WeightLogDto
			{
				Id = l.RowKey, PetId = l.PetId, Weight = l.Weight,
				Unit = l.WeightUnit, RecordedAt = l.RecordedAt,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified
			}).ToList(),
			VetInfo = vetInfos.Where(v => !v.IsDeleted).Select(v => new VetInfoDto
			{
				Id = v.RowKey, PetId = v.PetId, VetName = v.VetName,
				ClinicName = v.ClinicName, Phone = v.Phone,
				EmergencyPhone = v.EmergencyPhone, Address = v.Address,
				Email = v.Email, Notes = v.Notes, LastModified = v.LastModified
			}).FirstOrDefault(),
			Schedules = schedules.Where(s => !s.IsDeleted).Select(s => new ScheduleDto
			{
				Id = s.RowKey, PetId = s.PetId, ScheduleType = s.ScheduleType,
				Label = s.Label, TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified
			}).ToList()
		};

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	private static string GenerateCode(int length)
	{
		var random = Random.Shared;
		return new string(Enumerable.Range(0, length)
			.Select(_ => ShareCodeChars[random.Next(ShareCodeChars.Length)])
			.ToArray());
	}

	[Function("GetSharedUsers")]
	public async Task<HttpResponseData> GetSharedUsers(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/{code}/users")] HttpRequestData req,
		string code)
	{
		var shareCode = await _storage.GetShareCodeAsync(code);
		if (shareCode is null)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		var redemptions = await _storage.GetRedemptionsAsync(code);
		var result = new SharedUsersResponse
		{
			Users = redemptions.Select(r => new SharedUserDto
			{
				DeviceUserId = r.RowKey,
				DisplayName = r.DisplayName,
				AccessLevel = r.AccessLevel,
				RedeemedAt = r.RedeemedAt,
				IsRevoked = r.IsRevoked
			}).ToList()
		};

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	[Function("RevokeAccess")]
	public async Task<HttpResponseData> RevokeAccess(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "share/revoke")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<RevokeAccessRequest>();
		if (request is null || string.IsNullOrEmpty(request.ShareCode) || string.IsNullOrEmpty(request.DeviceUserId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var revoked = await _storage.RevokeRedemptionAsync(request.ShareCode, request.DeviceUserId);
		if (!revoked)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		_logger.LogInformation("Revoked access for {DeviceUserId} on share code {Code}", request.DeviceUserId, request.ShareCode);
		return req.CreateResponse(HttpStatusCode.OK);
	}

	[Function("DeleteShareCode")]
	public async Task<HttpResponseData> DeleteShareCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "share/{code}")] HttpRequestData req,
		string code)
	{
		var deleted = await _storage.DeleteShareCodeAsync(code);
		if (!deleted)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		_logger.LogInformation("Deleted share code {Code}", code);
		return req.CreateResponse(HttpStatusCode.OK);
	}
}
