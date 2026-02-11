namespace PetInsulinTracker.Shared.DTOs;

public class SyncRequest
{
	public string PetId { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
	public DateTimeOffset LastSyncTimestamp { get; set; }
	public List<PetDto> Pets { get; set; } = [];
	public List<InsulinLogDto> InsulinLogs { get; set; } = [];
	public List<FeedingLogDto> FeedingLogs { get; set; } = [];
	public List<WeightLogDto> WeightLogs { get; set; } = [];
	public List<MedicationLogDto> MedicationLogs { get; set; } = [];
	public List<VetInfoDto> VetInfos { get; set; } = [];
	public List<ScheduleDto> Schedules { get; set; } = [];
}

public class SyncResponse
{
	public DateTimeOffset SyncTimestamp { get; set; }
	public List<PetDto> Pets { get; set; } = [];
	public List<InsulinLogDto> InsulinLogs { get; set; } = [];
	public List<FeedingLogDto> FeedingLogs { get; set; } = [];
	public List<WeightLogDto> WeightLogs { get; set; } = [];
	public List<MedicationLogDto> MedicationLogs { get; set; } = [];
	public List<VetInfoDto> VetInfos { get; set; } = [];
	public List<ScheduleDto> Schedules { get; set; } = [];
}

public class ShareCodeRequest
{
	public string PetId { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	public string DeviceUserId { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
}

public class ShareCodeResponse
{
	public string ShareCode { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
}

public class ShareCodeDto
{
	public string Code { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	public DateTimeOffset CreatedAt { get; set; }
	public string CreatedById { get; set; } = string.Empty;
	public string CreatedByName { get; set; } = string.Empty;
}

public class ShareCodesResponse
{
	public List<ShareCodeDto> Codes { get; set; } = [];
}

public class RedeemShareCodeRequest
{
	public string ShareCode { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
}

public class RedeemShareCodeResponse
{
	public PetDto Pet { get; set; } = new();
	public List<InsulinLogDto> InsulinLogs { get; set; } = [];
	public List<FeedingLogDto> FeedingLogs { get; set; } = [];
	public List<WeightLogDto> WeightLogs { get; set; } = [];
	public List<MedicationLogDto> MedicationLogs { get; set; } = [];
	public VetInfoDto? VetInfo { get; set; }
	public List<ScheduleDto> Schedules { get; set; } = [];
}

public class SharedUserDto
{
	public string DeviceUserId { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	public DateTimeOffset RedeemedAt { get; set; }
	public bool IsRevoked { get; set; }
}

public class SharedUsersResponse
{
	public List<SharedUserDto> Users { get; set; } = [];
}

public class RevokeAccessRequest
{
	public string PetId { get; set; } = string.Empty;
	/// <summary>DeviceUserId of the user whose access is being revoked.</summary>
	public string DeviceUserId { get; set; } = string.Empty;
	/// <summary>DeviceUserId of the person making the revocation request (must be the pet owner).</summary>
	public string RequesterId { get; set; } = string.Empty;
}

public class LeavePetRequest
{
	public string PetId { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
}

public class DeletePetRequest
{
	public string PetId { get; set; } = string.Empty;
	public string OwnerId { get; set; } = string.Empty;
}

public class CreatePetRequest
{
	public string Id { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
	public string OwnerName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Species { get; set; } = string.Empty;
	public string Breed { get; set; } = string.Empty;
	public DateTime? DateOfBirth { get; set; }
	public string? InsulinType { get; set; }
	public string? InsulinConcentration { get; set; }
	public double? CurrentDoseIU { get; set; }
	public string WeightUnit { get; set; } = "lbs";
	public double? CurrentWeight { get; set; }
	public string? DefaultFoodName { get; set; }
	public double? DefaultFoodAmount { get; set; }
	public string DefaultFoodUnit { get; set; } = "cups";
	public string DefaultFoodType { get; set; } = "Dry";
}

public class CreatePetResponse
{
	public PetDto Pet { get; set; } = new();
}

public class PetPhotoUploadRequest
{
	public string PetId { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
	public string Base64Image { get; set; } = string.Empty;
}

public class PetPhotoUploadResponse
{
	public string PhotoUrl { get; set; } = string.Empty;
}
