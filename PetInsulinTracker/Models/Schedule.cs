using SQLite;

namespace PetInsulinTracker.Models;

public class Schedule
{
	[PrimaryKey]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[Indexed]
	public string PetId { get; set; } = string.Empty;

	/// <summary>Insulin or Feeding</summary>
	public string ScheduleType { get; set; } = "Insulin";

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

	/// <summary>Interval in hours between doses/feedings (e.g., 12 for twice daily)</summary>
	public int IntervalHours { get; set; } = 12;

	public bool IsEnabled { get; set; } = true;

	/// <summary>Minutes before scheduled time to send reminder</summary>
	public int ReminderLeadTimeMinutes { get; set; } = 15;

	public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

	public bool IsSynced { get; set; }
}
