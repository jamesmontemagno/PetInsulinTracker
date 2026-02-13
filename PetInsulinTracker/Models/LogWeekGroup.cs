using System.Globalization;

namespace PetInsulinTracker.Models;

/// <summary>
/// Groups log entries by week for display in history lists.
/// </summary>
public class LogWeekGroup<T> : List<T>
{
	public string WeekLabel { get; }

	public LogWeekGroup(string weekLabel, IEnumerable<T> items) : base(items)
	{
		WeekLabel = weekLabel;
	}

	/// <summary>
	/// Groups a list of items by week, with friendly labels like "This Week", "Last Week", etc.
	/// Items are assumed to be sorted newest-first. The date selector extracts the date from each item.
	/// When <paramref name="recentOnly"/> is true, only items from the last 30 days are included.
	/// </summary>
	public static List<LogWeekGroup<T>> GroupByWeek(
		IEnumerable<T> items,
		Func<T, DateTime> dateSelector,
		bool recentOnly = true)
	{
		var now = DateTime.Now;
		var cutoff = recentOnly ? now.AddDays(-30) : DateTime.MinValue;

		var filtered = items.Where(i => dateSelector(i) >= cutoff);

		var groups = filtered
			.GroupBy(i => GetWeekStart(dateSelector(i)))
			.OrderByDescending(g => g.Key)
			.Select(g => new LogWeekGroup<T>(GetWeekLabel(g.Key, now), g))
			.ToList();

		return groups;
	}

	private static DateTime GetWeekStart(DateTime date)
	{
		var diff = (7 + (date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)) % 7;
		return date.Date.AddDays(-diff);
	}

	private static string GetWeekLabel(DateTime weekStart, DateTime now)
	{
		var currentWeekStart = GetWeekStart(now);

		if (weekStart == currentWeekStart)
			return "This Week";

		if (weekStart == currentWeekStart.AddDays(-7))
			return "Last Week";

		var weekEnd = weekStart.AddDays(6);
		return $"{weekStart:MMM d} – {weekEnd:MMM d}";
	}
}
