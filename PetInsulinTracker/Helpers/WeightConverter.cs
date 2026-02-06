namespace PetInsulinTracker.Helpers;

public static class WeightConverter
{
	private const double LbsPerKg = 2.20462;

	public static double Convert(double weight, string fromUnit, string toUnit)
	{
		if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
			return weight;
		if (string.Equals(fromUnit, "lbs", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(toUnit, "kg", StringComparison.OrdinalIgnoreCase))
			return Math.Round(weight / LbsPerKg, 2);
		if (string.Equals(fromUnit, "kg", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(toUnit, "lbs", StringComparison.OrdinalIgnoreCase))
			return Math.Round(weight * LbsPerKg, 2);
		return weight;
	}

	public static string PreferredUnit => Preferences.Get("default_weight_unit", "lbs");
}
