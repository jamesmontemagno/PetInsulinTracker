using SQLite;

namespace PetInsulinTracker.Models;

public class FeedingLog
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	public string FoodName { get; set; } = string.Empty;

	public double Amount { get; set; }

	/// <summary>cups, grams, oz, or cans</summary>
	public string Unit { get; set; } = "cups";

	/// <summary>Wet, Dry, or Treat</summary>
	public string FoodType { get; set; } = "Dry";

	public DateTime FedAt { get; set; } = DateTime.Now;

	public string? Notes { get; set; }

	/// <summary>Name of the person who logged this entry</summary>
	public string? LoggedBy { get; set; }

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
