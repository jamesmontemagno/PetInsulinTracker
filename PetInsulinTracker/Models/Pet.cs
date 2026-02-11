using SQLite;

namespace PetInsulinTracker.Models;

public class Pet
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>DeviceUserId (GUID) of the person who owns/created this pet</summary>
	public string? OwnerId { get; set; }

	/// <summary>Display name of the owner</summary>
	public string? OwnerName { get; set; }

	/// <summary>Access level: owner, full, or guest</summary>
	public string AccessLevel { get; set; } = "owner";

	public string Name { get; set; } = string.Empty;

	/// <summary>Cat or Dog</summary>
	public string Species { get; set; } = string.Empty;

	public string Breed { get; set; } = string.Empty;

	public DateTime? DateOfBirth { get; set; }

	public string? PhotoPath { get; set; }
	public string? PhotoUrl { get; set; }

	[Ignore]
	public string? PhotoSource
	{
		get
		{
			var preferLocal = Preferences.Get("prefer_local_image", true);

			if (preferLocal && !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath))
				return PhotoPath;

			if (!string.IsNullOrEmpty(PhotoUrl))
				return CacheBustedUrl(PhotoUrl);

			// Fallback: use local path even if preference is off (no remote available)
			if (!string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath))
				return PhotoPath;

			return null;
		}
	}

	/// <summary>
	/// Appends a cache-busting query parameter to a remote URL using LastModified,
	/// so MAUI's image cache treats re-uploaded photos as new resources.
	/// </summary>
	private string CacheBustedUrl(string url)
	{
		var separator = url.Contains('?') ? "&" : "?";
		return $"{url}{separator}v={LastModified.Ticks}";
	}

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

	/// <summary>Default food name for quick logging, e.g., "Royal Canin Diabetic"</summary>
	public string? DefaultFoodName { get; set; }

	/// <summary>Default food amount per meal</summary>
	public double? DefaultFoodAmount { get; set; }

	/// <summary>cups, grams, oz, or cans</summary>
	public string DefaultFoodUnit { get; set; } = "cups";

	/// <summary>Wet, Dry, or Treat</summary>
	public string DefaultFoodType { get; set; } = "Dry";

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }

	public bool IsDeleted { get; set; }
}
