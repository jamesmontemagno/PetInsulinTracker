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
	public List<VetInfoDto> VetInfos { get; set; } = [];
	public List<ScheduleDto> Schedules { get; set; } = [];
}

public class ShareCodeRequest
{
	public string PetId { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	public string OwnerId { get; set; } = string.Empty;
}

public class ShareCodeResponse
{
	public string ShareCode { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
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
	public string ShareCode { get; set; } = string.Empty;
	public string DeviceUserId { get; set; } = string.Empty;
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
}

public class CreatePetResponse
{
	public PetDto Pet { get; set; } = new();
}
