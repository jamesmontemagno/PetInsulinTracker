using Azure;
using Azure.Data.Tables;

namespace PetInsulinTracker.Api.Models;

public class PetEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string? OwnerId { get; set; }
	public string? OwnerName { get; set; }
	public string AccessLevel { get; set; } = "owner";
	public string Name { get; set; } = string.Empty;
	public string Species { get; set; } = string.Empty;
	public string Breed { get; set; } = string.Empty;
	public DateTime? DateOfBirth { get; set; }
	public string? PhotoUrl { get; set; }
	public string? InsulinType { get; set; }
	public string? InsulinConcentration { get; set; }
	public double? CurrentDoseIU { get; set; }
	public string WeightUnit { get; set; } = "lbs";
	public double? CurrentWeight { get; set; }
	public string? DefaultFoodName { get; set; }
	public double? DefaultFoodAmount { get; set; }
	public string DefaultFoodUnit { get; set; } = "cups";
	public string DefaultFoodType { get; set; } = "Dry";
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class InsulinLogEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public double DoseIU { get; set; }
	public DateTime AdministeredAt { get; set; }
	public string? InjectionSite { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public string? LoggedById { get; set; }
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class FeedingLogEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public string FoodName { get; set; } = string.Empty;
	public double Amount { get; set; }
	public string Unit { get; set; } = "cups";
	public string FoodType { get; set; } = "Dry";
	public DateTime FedAt { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public string? LoggedById { get; set; }
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class WeightLogEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public double Weight { get; set; }
	public string WeightUnit { get; set; } = "lbs";
	public DateTime RecordedAt { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public string? LoggedById { get; set; }
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class MedicationLogEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public string MedicationName { get; set; } = string.Empty;
	public DateTime AdministeredAt { get; set; }
	public string? Notes { get; set; }
	public string? LoggedBy { get; set; }
	public string? LoggedById { get; set; }
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class VetInfoEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public string VetName { get; set; } = string.Empty;
	public string ClinicName { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? EmergencyPhone { get; set; }
	public string? Address { get; set; }
	public string? Email { get; set; }
	public string? Notes { get; set; }
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class ScheduleEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty;
	public string RowKey { get; set; } = string.Empty;
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public string ScheduleType { get; set; } = "Insulin";
	public string Label { get; set; } = string.Empty;
	public long TimeTicks { get; set; }
	public bool IsEnabled { get; set; } = true;
	public int ReminderLeadTimeMinutes { get; set; } = 15;
	public DateTimeOffset LastModified { get; set; }
	public bool IsDeleted { get; set; }
}

public class ShareCodeEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty; // PetId
	public string RowKey { get; set; } = string.Empty; // The share code itself
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string PetId { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	/// <summary>
	/// Legacy field: the pet owner's ID at code creation time.
	/// Retained for backward compatibility with existing table data.
	/// Prefer <see cref="CreatedById"/> for identifying who created the code.
	/// </summary>
	public string? OwnerId { get; set; }
	public string CreatedById { get; set; } = string.Empty;
	public string CreatedByName { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ShareRedemptionEntity : ITableEntity
{
	public string PartitionKey { get; set; } = string.Empty; // PetId
	public string RowKey { get; set; } = string.Empty; // Redeemer's DeviceUserId
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	public string ShareCode { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;
	public string AccessLevel { get; set; } = "full";
	public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;
	public bool IsRevoked { get; set; }
}
