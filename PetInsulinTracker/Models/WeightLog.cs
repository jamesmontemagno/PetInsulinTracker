using SQLite;

namespace PetInsulinTracker.Models;

public class WeightLog
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	public double Weight { get; set; }

	/// <summary>lbs or kg</summary>
	public string Unit { get; set; } = "lbs";

	public DateTime RecordedAt { get; set; } = DateTime.Now;

	public string? Notes { get; set; }

	/// <summary>Name of the person who logged this entry</summary>
	public string? LoggedBy { get; set; }

	/// <summary>Stable unique ID of the person who logged this entry</summary>
	public string? LoggedById { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
