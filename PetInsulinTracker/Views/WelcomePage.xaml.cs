using PetInsulinTracker.Helpers;
using PetInsulinTracker.Themes;

namespace PetInsulinTracker.Views;

public partial class WelcomePage : ContentPage
{
	private readonly List<ThemePreview> _themes;

	public WelcomePage()
	{
		InitializeComponent();

		_themes =
		[
			new("Warm & Earthy",  Themes.AppTheme.Warm,     "#E8910C", "#F28B6E", "#FDF5EC", "#4A3728", "#FFFFFF", "#3D2C1E"),
			new("Ocean Breeze",   Themes.AppTheme.Ocean,    "#0288D1", "#26C6DA", "#E8F4F8", "#01579B", "#FFFFFF", "#1A3A4A"),
			new("Forest Walk",    Themes.AppTheme.Forest,   "#2E7D32", "#8D6E63", "#E8F0E0", "#1B5E20", "#FFFFFF", "#1B3A1E"),
			new("Berry Bliss",    Themes.AppTheme.Berry,    "#AD1457", "#7B1FA2", "#FCE4EC", "#880E4F", "#FFFFFF", "#3D1A28"),
			new("Midnight Indigo",Themes.AppTheme.Midnight, "#5C6BC0", "#26C6DA", "#E8EAF6", "#283593", "#FFFFFF", "#1A1F3D"),
		];

		ThemeCarousel.ItemsSource = _themes;
		ThemeCarousel.IndicatorView = ThemeIndicator;
		ThemeCarousel.Position = 3;
	}

	private void OnThemeCarouselChanged(object? sender, CurrentItemChangedEventArgs e)
	{
		if (e.CurrentItem is ThemePreview preview)
		{
			foreach (var t in _themes)
				t.BorderColor = Colors.Transparent;
			preview.BorderColor = Color.FromArgb(preview.PrimaryHex);
			ThemeService.ApplyTheme(preview.Theme);
		}
	}

	private void GoToStep(int step)
	{
		Step1.IsVisible = step == 1;
		Step2.IsVisible = step == 2;
		Step3.IsVisible = step == 3;
		Dot1.Color = step >= 1 ? GetPrimaryColor() : GetDividerColor();
		Dot2.Color = step >= 2 ? GetPrimaryColor() : GetDividerColor();
		Dot3.Color = step >= 3 ? GetPrimaryColor() : GetDividerColor();
	}

	private static Color GetPrimaryColor() =>
		Application.Current?.Resources.TryGetValue("CurrentPrimary", out var c) == true && c is Color color
			? color : Color.FromArgb("#AD1457");

	private static Color GetDividerColor() =>
		Application.Current?.Resources.TryGetValue("CurrentDivider", out var c) == true && c is Color color
			? color : Color.FromArgb("#E8D0D8");

	// Step 1 → Step 2
	private async void OnStep1Next(object? sender, EventArgs e)
	{
		var name = NameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlertAsync("Name Required", "Please enter your name to continue.", "OK");
			return;
		}
		Preferences.Set(Constants.OwnerNameKey, name);
		GoToStep(2);
	}

	// Step 2 → Step 3 (save schedule prefs)
	private void OnStep2Next(object? sender, EventArgs e)
	{
		SaveSchedulePreferences();
		GoToStep(3);
	}

	// Step 2 skip → Step 3
	private void OnStep2Skip(object? sender, EventArgs e)
	{
		GoToStep(3);
	}

	// Step 3 done (save vet prefs + finish)
	private void OnStep3Done(object? sender, EventArgs e)
	{
		SaveVetPreferences();
		FinishOnboarding();
	}

	// Step 3 skip → finish
	private void OnStep3Skip(object? sender, EventArgs e)
	{
		FinishOnboarding();
	}

	private void SaveSchedulePreferences()
	{
		if (MorningInsulinEnabled.IsToggled)
		{
			Preferences.Set("onboard_morning_insulin_time", (MorningInsulinTime.Time ?? new TimeSpan(7, 0, 0)).Ticks);
			Preferences.Set("onboard_morning_insulin_reminder",
				int.TryParse(MorningInsulinReminder.Text, out var r) ? r : 15);
		}
		if (EveningInsulinEnabled.IsToggled)
		{
			Preferences.Set("onboard_evening_insulin_time", (EveningInsulinTime.Time ?? new TimeSpan(19, 0, 0)).Ticks);
			Preferences.Set("onboard_evening_insulin_reminder",
				int.TryParse(EveningInsulinReminder.Text, out var r) ? r : 15);
		}
		if (MorningFeedingEnabled.IsToggled)
		{
			Preferences.Set("onboard_morning_feeding_time", (MorningFeedingTime.Time ?? new TimeSpan(7, 0, 0)).Ticks);
			Preferences.Set("onboard_morning_feeding_reminder",
				int.TryParse(MorningFeedingReminder.Text, out var r) ? r : 15);
		}
		if (EveningFeedingEnabled.IsToggled)
		{
			Preferences.Set("onboard_evening_feeding_time", (EveningFeedingTime.Time ?? new TimeSpan(19, 0, 0)).Ticks);
			Preferences.Set("onboard_evening_feeding_reminder",
				int.TryParse(EveningFeedingReminder.Text, out var r) ? r : 15);
		}
		Preferences.Set("onboard_has_schedules", true);
	}

	private void SaveVetPreferences()
	{
		var vetName = VetNameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(vetName)) return;

		Preferences.Set("onboard_vet_name", vetName);
		Preferences.Set("onboard_clinic_name", ClinicNameEntry.Text?.Trim() ?? "");
		Preferences.Set("onboard_vet_phone", VetPhoneEntry.Text?.Trim() ?? "");
		Preferences.Set("onboard_emergency_phone", EmergencyPhoneEntry.Text?.Trim() ?? "");
		Preferences.Set("onboard_vet_address", VetAddressEntry.Text?.Trim() ?? "");
		Preferences.Set("onboard_has_vet", true);
	}

	private void FinishOnboarding()
	{
		Preferences.Set("setup_complete", true);

		if (Application.Current is not null)
			Application.Current.Windows[0].Page = new AppShell();
	}
}

public class ThemePreview : BindableObject
{
	public string Name { get; }
	public Themes.AppTheme Theme { get; }
	public string PrimaryHex { get; }
	public Color Primary { get; }
	public Color Secondary { get; }
	public Color Background { get; }
	public Color Shell { get; }
	public Color Card { get; }
	public Color TextColor { get; }

	public static readonly BindableProperty BorderColorProperty =
		BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(ThemePreview), Colors.Transparent);

	public Color BorderColor
	{
		get => (Color)GetValue(BorderColorProperty);
		set => SetValue(BorderColorProperty, value);
	}

	public ThemePreview(string name, Themes.AppTheme theme, string primary, string secondary,
		string bg, string shell, string card, string text)
	{
		Name = name;
		Theme = theme;
		PrimaryHex = primary;
		Primary = Color.FromArgb(primary);
		Secondary = Color.FromArgb(secondary);
		Background = Color.FromArgb(bg);
		Shell = Color.FromArgb(shell);
		Card = Color.FromArgb(card);
		TextColor = Color.FromArgb(text);
	}
}
