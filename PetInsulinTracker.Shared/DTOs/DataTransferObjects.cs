namespace PetInsulinTracker.Shared.DTOs;

public class PetDto
{
	public string Id { get; set; } = string.Empty;
	public string? OwnerId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Species { get; set; } = string.Empty;
	public string Breed { get; set; } = string.Empty;
	public DateTime? DateOfBirth { get; set; }
	public string? InsulinType { get; set; }
	public string? InsulinConcentration { get; set; }
	public double? CurrentDoseIU { get; set; }
	public string WeightUnit { get; set; } = "lbs";
	public double? CurrentWeight { get; set; }
	public string? ShareCode { get; set; }
	public string AccessLevel { get; set; } = "owner";
	public DateTimeOffset LastModified { get; set; }
}

public class InsulinLogDto
{
	public string Id { get; set; } = string.Empty;
	public string PetId { get; set; } = string.Empty;
	public double DoseIU { get; set; }
	public DateTime AdministeredAt { get; set; }
	public string? InjectionSite { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public DateTimeOffset LastModified { get; set; }
}

public class FeedingLogDto
{
	public string Id { get; set; } = string.Empty;
	public string PetId { get; set; } = string.Empty;
	public string FoodName { get; set; } = string.Empty;
	public double Amount { get; set; }
	public string Unit { get; set; } = "cups";
	public string FoodType { get; set; } = "Dry";
	public DateTime FedAt { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public DateTimeOffset LastModified { get; set; }
}

public class WeightLogDto
{
	public string Id { get; set; } = string.Empty;
	public string PetId { get; set; } = string.Empty;
	public double Weight { get; set; }
	public string Unit { get; set; } = "lbs";
	public DateTime RecordedAt { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public DateTimeOffset LastModified { get; set; }
}

public class VetInfoDto
{
	public string Id { get; set; } = string.Empty;
	public string PetId { get; set; } = string.Empty;
	public string VetName { get; set; } = string.Empty;
	public string ClinicName { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? EmergencyPhone { get; set; }
	public string? Address { get; set; }
	public string? Email { get; set; }
	public string? Notes { get; set; }
	public DateTimeOffset LastModified { get; set; }
}

public class ScheduleDto
{
	public string Id { get; set; } = string.Empty;
	public string PetId { get; set; } = string.Empty;
	public string ScheduleType { get; set; } = "Insulin";
	public string Label { get; set; } = string.Empty;
	public long TimeTicks { get; set; }
	public bool IsEnabled { get; set; } = true;
	public int ReminderLeadTimeMinutes { get; set; } = 15;
	public DateTimeOffset LastModified { get; set; }
}
