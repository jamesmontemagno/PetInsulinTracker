using PetInsulinTracker.Helpers;
using SQLite;

namespace PetInsulinTracker.Models;

public class Schedule
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	/// <summary>Insulin, Feeding, or Insulin & Feeding</summary>
	public string ScheduleType { get; set; } = Constants.ScheduleTypeInsulin;

	/// <summary>Display label, e.g., "Morning Insulin"</summary>
	public string Label { get; set; } = string.Empty;

	/// <summary>Time of day (stored as ticks)</summary>
	public long TimeTicks { get; set; }

	[Ignore]
	public TimeSpan TimeOfDay
	{
		get => TimeSpan.FromTicks(TimeTicks);
		set => TimeTicks = value.Ticks;
	}

	public bool IsEnabled { get; set; } = true;

	/// <summary>Minutes before scheduled time to send reminder</summary>
	public int ReminderLeadTimeMinutes { get; set; } = 15;

	[Ignore]
	public string TimeDisplay => DateTime.Today.Add(TimeOfDay).ToString("h:mm tt");

	[Ignore]
	public string ReminderDisplay => $"Remind {ReminderLeadTimeMinutes} min before";

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }

	public bool IsDeleted { get; set; }
}
