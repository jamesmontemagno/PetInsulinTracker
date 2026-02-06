using SQLite;

namespace PetInsulinTracker.Models;

public class Pet
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string Name { get; set; } = string.Empty;

	/// <summary>Cat or Dog</summary>
	public string Species { get; set; } = string.Empty;

	public string Breed { get; set; } = string.Empty;

	public DateTime? DateOfBirth { get; set; }

	public string? PhotoPath { get; set; }

	/// <summary>e.g., Vetsulin, ProZinc, NPH, Glargine</summary>
	public string? InsulinType { get; set; }

	/// <summary>U-40 or U-100</summary>
	public string? InsulinConcentration { get; set; }

	/// <summary>Current prescribed dose in IU</summary>
	public double? CurrentDoseIU { get; set; }

	/// <summary>lbs or kg</summary>
	public string WeightUnit { get; set; } = "lbs";

	/// <summary>Current weight value</summary>
	public double? CurrentWeight { get; set; }

	/// <summary>Share code for syncing with others</summary>
	public string? ShareCode { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
