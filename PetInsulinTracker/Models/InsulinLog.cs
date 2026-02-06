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

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
