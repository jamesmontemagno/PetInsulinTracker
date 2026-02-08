using SQLite;

namespace PetInsulinTracker.Models;

public class InsulinLog
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	/// <summary>Dose in International Units (IU)</summary>
	public double DoseIU { get; set; }

	public DateTime AdministeredAt { get; set; } = DateTime.Now;

	/// <summary>e.g., scruff, flank, behind ear</summary>
	public string? InjectionSite { get; set; }

	public string? Notes { get; set; }

	/// <summary>Name of the person who logged this entry</summary>
	public string? LoggedBy { get; set; }

	/// <summary>Stable unique ID of the person who logged this entry</summary>
	public string? LoggedById { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }

	public bool IsDeleted { get; set; }
}
