namespace PetInsulinTracker.Converters;

public class EqualityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		if (value is null || parameter is null) return false;
		return value.ToString() == parameter.ToString();
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
