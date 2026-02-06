using SQLite;

namespace PetInsulinTracker.Models;

public class VetInfo
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	public string VetName { get; set; } = string.Empty;

	public string ClinicName { get; set; } = string.Empty;

	public string? Phone { get; set; }

	public string? EmergencyPhone { get; set; }

	public string? Address { get; set; }

	public string? Email { get; set; }

	public string? Notes { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
