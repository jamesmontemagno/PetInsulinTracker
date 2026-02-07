namespace PetInsulinTracker.Shared.DTOs;

public class SyncRequest
{
	public string ShareCode { get; set; } = string.Empty;
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
}

public class ShareCodeResponse
{
	public string ShareCode { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
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
