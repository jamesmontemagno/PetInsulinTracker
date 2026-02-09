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
	private const string FullAccessLevel = "full";

	public ShareFunctions(ILogger<ShareFunctions> logger, TableStorageService storage)
	{
		_logger = logger;
		_storage = storage;
	}

	private static string? GetQueryValue(Uri url, string key)
	{
		var query = url.Query;
		if (string.IsNullOrEmpty(query)) return null;
		foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var kvp = part.Split('=', 2);
			if (kvp.Length == 2
				&& string.Equals(Uri.UnescapeDataString(kvp[0]), key, StringComparison.OrdinalIgnoreCase))
			{
				return Uri.UnescapeDataString(kvp[1]);
			}
		}
		return null;
	}

	private async Task<bool> HasOwnerOrFullAccessAsync(string petId, string deviceUserId)
	{
		var pet = await _storage.GetPetAsync(petId);
		if (pet is null) return false;
		if (pet.OwnerId == deviceUserId) return true;
		var redemption = await _storage.GetRedemptionAsync(petId, deviceUserId);
		return redemption is not null
			&& !redemption.IsRevoked
			&& redemption.AccessLevel == FullAccessLevel;
	}

	[Function("GenerateShareCode")]
	public async Task<HttpResponseData> GenerateShareCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "share/generate")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<ShareCodeRequest>();
		if (request is null || string.IsNullOrEmpty(request.PetId))
		{
			_logger.LogWarning("GenerateShareCode rejected: missing PetId");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (string.IsNullOrEmpty(request.DeviceUserId))
		{
			_logger.LogWarning("GenerateShareCode rejected: missing DeviceUserId for pet {PetId}", request.PetId);
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		_logger.LogInformation(
			"GenerateShareCode for pet {PetId} by user {DeviceUserId}, access={AccessLevel}",
			request.PetId, request.DeviceUserId, request.AccessLevel);

		var pet = await _storage.GetPetAsync(request.PetId);
		if (pet is null)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		// Inline access check to avoid redundant pet lookup via HasOwnerOrFullAccessAsync
		if (pet.OwnerId != request.DeviceUserId)
		{
			var redemption = await _storage.GetRedemptionAsync(request.PetId, request.DeviceUserId);
			var hasFullAccess = redemption is not null
				&& !redemption.IsRevoked
				&& redemption.AccessLevel == FullAccessLevel;
			if (!hasFullAccess)
			{
				_logger.LogWarning(
					"Unauthorized user {RequesterId} attempted to generate share code for pet {PetId}",
					request.DeviceUserId, request.PetId);
				return req.CreateResponse(HttpStatusCode.Forbidden);
			}
		}

		// Generate a unique 6-character code (no ambiguous chars)
		string code;
		do
		{
			code = GenerateCode(6);
		}
		while (await _storage.GetShareCodeAsync(code) is not null);

		var creatorName = string.IsNullOrWhiteSpace(request.DisplayName)
			? request.DeviceUserId
			: request.DisplayName;

		await _storage.CreateShareCodeAsync(
			code,
			request.PetId,
			request.AccessLevel,
			pet.OwnerId,
			request.DeviceUserId,
			creatorName);
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
			_logger.LogWarning("RedeemShareCode rejected: missing ShareCode");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		_logger.LogInformation("RedeemShareCode request: Code={Code}, DeviceUserId={DeviceUserId}", request.ShareCode, request.DeviceUserId);

		var code = request.ShareCode;
		var shareCode = await _storage.GetShareCodeAsync(code);
		if (shareCode is null)
		{
			_logger.LogWarning("Share code {Code} not found", code);
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		_logger.LogDebug("Share code {Code} found: PetId={PetId}, AccessLevel={AccessLevel}", code, shareCode.PetId, shareCode.AccessLevel);

		var accessLevel = shareCode.AccessLevel ?? "full";
		var petId = shareCode.PetId;

		// Record who redeemed
		if (!string.IsNullOrEmpty(request.DeviceUserId))
		{
			await _storage.CreateRedemptionAsync(petId, code, request.DeviceUserId, request.DisplayName, accessLevel);
		}

		// Get pet data by petId
		var pet = await _storage.GetPetAsync(petId);
		if (pet is null || pet.IsDeleted)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		var insulinLogs = await _storage.GetEntitiesByPartitionAsync<Models.InsulinLogEntity>("InsulinLogs", petId);
		var feedingLogs = await _storage.GetEntitiesByPartitionAsync<Models.FeedingLogEntity>("FeedingLogs", petId);
		var weightLogs = await _storage.GetEntitiesByPartitionAsync<Models.WeightLogEntity>("WeightLogs", petId);
		var vetInfos = await _storage.GetEntitiesByPartitionAsync<Models.VetInfoEntity>("VetInfos", petId);
		var schedules = await _storage.GetEntitiesByPartitionAsync<Models.ScheduleEntity>("Schedules", petId);

		var result = new RedeemShareCodeResponse
		{
			Pet = new PetDto
			{
				Id = pet.RowKey,
				OwnerId = pet.OwnerId,
				OwnerName = pet.OwnerName,
				AccessLevel = accessLevel,
				Name = pet.Name,
				Species = pet.Species,
				Breed = pet.Breed,
				DateOfBirth = pet.DateOfBirth,
				PhotoUrl = pet.PhotoUrl,
				InsulinType = pet.InsulinType,
				InsulinConcentration = pet.InsulinConcentration,
				CurrentDoseIU = pet.CurrentDoseIU,
				WeightUnit = pet.WeightUnit,
				CurrentWeight = pet.CurrentWeight,
				DefaultFoodName = pet.DefaultFoodName,
				DefaultFoodAmount = pet.DefaultFoodAmount,
				DefaultFoodUnit = pet.DefaultFoodUnit,
				DefaultFoodType = pet.DefaultFoodType,
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

		_logger.LogInformation(
			"RedeemShareCode success: Pet={PetId}, InsulinLogs={InsulinCount}, FeedingLogs={FeedingCount}, WeightLogs={WeightCount}, Schedules={ScheduleCount}",
			petId, result.InsulinLogs.Count, result.FeedingLogs.Count, result.WeightLogs.Count, result.Schedules.Count);

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	[Function("GetShareCodes")]
	public async Task<HttpResponseData> GetShareCodes(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/pet/{petId}/codes")] HttpRequestData req,
		string petId)
	{
		var deviceUserId = GetQueryValue(req.Url, "deviceUserId");
		if (string.IsNullOrEmpty(deviceUserId))
			return req.CreateResponse(HttpStatusCode.BadRequest);

		var canManage = await HasOwnerOrFullAccessAsync(petId, deviceUserId);
		if (!canManage)
			return req.CreateResponse(HttpStatusCode.Forbidden);

		var codes = await _storage.GetShareCodesByPetIdAsync(petId);
		var result = new ShareCodesResponse
		{
			Codes = codes.Select(c =>
			{
				var createdById = string.IsNullOrWhiteSpace(c.CreatedById) ? c.OwnerId ?? string.Empty : c.CreatedById;
				var createdByName = string.IsNullOrWhiteSpace(c.CreatedByName) ? createdById : c.CreatedByName;
				return new ShareCodeDto
				{
					Code = c.RowKey,
					AccessLevel = c.AccessLevel,
					CreatedAt = c.CreatedAt,
					CreatedById = createdById,
					CreatedByName = createdByName
				};
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
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/pet/{petId}/users")] HttpRequestData req,
		string petId)
	{
		var deviceUserId = GetQueryValue(req.Url, "deviceUserId");
		if (string.IsNullOrEmpty(deviceUserId))
			return req.CreateResponse(HttpStatusCode.BadRequest);

		var canManage = await HasOwnerOrFullAccessAsync(petId, deviceUserId);
		if (!canManage)
			return req.CreateResponse(HttpStatusCode.Forbidden);

		var redemptions = await _storage.GetRedemptionsByPetAsync(petId);
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
		if (request is null || string.IsNullOrEmpty(request.PetId) || string.IsNullOrEmpty(request.DeviceUserId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		// Validate the requester is the pet owner
		if (string.IsNullOrEmpty(request.RequesterId))
			return req.CreateResponse(HttpStatusCode.BadRequest);

		var pet = await _storage.GetPetAsync(request.PetId);
		if (pet is null)
			return req.CreateResponse(HttpStatusCode.NotFound);

		if (pet.OwnerId != request.RequesterId)
		{
			_logger.LogWarning("Unauthorized revoke attempt by {RequesterId} on pet {PetId}",
				request.RequesterId, request.PetId);
			return req.CreateResponse(HttpStatusCode.Forbidden);
		}

		var revoked = await _storage.RevokeRedemptionAsync(request.PetId, request.DeviceUserId);
		if (!revoked)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		_logger.LogInformation("Revoked access for {DeviceUserId} on pet {PetId} by {RequesterId}",
			request.DeviceUserId, request.PetId, request.RequesterId);
		return req.CreateResponse(HttpStatusCode.OK);
	}

	/// <summary>
	/// Self-service endpoint: the DeviceUserId in the request body identifies the user leaving.
	/// No separate requester validation is performed since the DeviceUserId serves as both
	/// the identity claim and the target. This is consistent with the app's trust model
	/// where DeviceUserId is treated as a trusted client-provided identifier.
	/// </summary>
	[Function("LeavePet")]
	public async Task<HttpResponseData> LeavePet(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "share/leave")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<LeavePetRequest>();
		if (request is null || string.IsNullOrEmpty(request.PetId) || string.IsNullOrEmpty(request.DeviceUserId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var removed = await _storage.DeleteRedemptionAsync(request.PetId, request.DeviceUserId);
		if (!removed)
			return req.CreateResponse(HttpStatusCode.NotFound);

		_logger.LogInformation("Removed redemption for {DeviceUserId} on pet {PetId}", request.DeviceUserId, request.PetId);
		return req.CreateResponse(HttpStatusCode.OK);
	}

	[Function("DeleteShareCode")]
	public async Task<HttpResponseData> DeleteShareCode(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "share/{code}")] HttpRequestData req,
		string code)
	{
		var deviceUserId = GetQueryValue(req.Url, "deviceUserId");
		if (string.IsNullOrEmpty(deviceUserId))
			return req.CreateResponse(HttpStatusCode.BadRequest);

		var shareCode = await _storage.GetShareCodeAsync(code);
		if (shareCode is null)
			return req.CreateResponse(HttpStatusCode.NotFound);

		var canManage = await HasOwnerOrFullAccessAsync(shareCode.PetId, deviceUserId);
		if (!canManage)
			return req.CreateResponse(HttpStatusCode.Forbidden);

		var deleted = await _storage.DeleteShareCodeAsync(code);
		if (!deleted)
		{
			return req.CreateResponse(HttpStatusCode.NotFound);
		}

		_logger.LogInformation("Deleted share code {Code}", code);
		return req.CreateResponse(HttpStatusCode.OK);
	}
}
