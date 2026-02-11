using SQLite;

namespace PetInsulinTracker.Models;

public class MedicationLog
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	/// <summary>Free-form medication name/description, e.g. "Pred 5mg"</summary>
	public string MedicationName { get; set; } = string.Empty;

	public DateTime AdministeredAt { get; set; } = DateTime.Now;

	public string? Notes { get; set; }

	/// <summary>Name of the person who logged this entry</summary>
	public string? LoggedBy { get; set; }

	/// <summary>Stable unique ID of the person who logged this entry</summary>
	public string? LoggedById { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }

	public bool IsDeleted { get; set; }
}
