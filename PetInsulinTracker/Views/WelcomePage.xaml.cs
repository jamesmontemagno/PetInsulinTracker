using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using PetInsulinTracker.Themes;

namespace PetInsulinTracker.Views;

public partial class WelcomePage : ContentPage
{
	private readonly List<ThemePreview> _themes;
	private string? _savedPetId;

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

		// Default weight unit from preferences
		WeightUnitPicker.SelectedItem = Preferences.Get("default_weight_unit", "lbs");
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
		Step4.IsVisible = step == 4;
		Dot1.Color = step >= 1 ? GetPrimaryColor() : GetDividerColor();
		Dot2.Color = step >= 2 ? GetPrimaryColor() : GetDividerColor();
		Dot3.Color = step >= 3 ? GetPrimaryColor() : GetDividerColor();
		Dot4.Color = step >= 4 ? GetPrimaryColor() : GetDividerColor();
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

	// Step 2: Save pet and continue to schedules
	private async void OnStep2Next(object? sender, EventArgs e)
	{
		var petName = PetNameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(petName))
		{
			await DisplayAlertAsync("Pet Name Required", "Please enter your pet's name to continue.", "OK");
			return;
		}

		await SavePetAsync(petName);
		GoToStep(3);
	}

	// Step 2 skip → finish (no pet = no schedules/vet needed)
	private void OnStep2Skip(object? sender, EventArgs e)
	{
		FinishOnboarding();
	}

	// Step 3: Save schedules → Step 4
	private async void OnStep3Next(object? sender, EventArgs e)
	{
		await SaveSchedulesAsync();
		GoToStep(4);
	}

	// Step 3 skip → Step 4
	private void OnStep3Skip(object? sender, EventArgs e)
	{
		GoToStep(4);
	}

	// Step 4 done (save vet + finish)
	private async void OnStep4Done(object? sender, EventArgs e)
	{
		await SaveVetInfoAsync();
		FinishOnboarding();
	}

	// Step 4 skip → finish
	private void OnStep4Skip(object? sender, EventArgs e)
	{
		FinishOnboarding();
	}

	private async Task SavePetAsync(string petName)
	{
		var db = GetDatabaseService();

		var pet = new Pet
		{
			Name = petName,
			Species = SpeciesPicker.SelectedItem as string ?? "Dog",
			Breed = BreedEntry.Text?.Trim() ?? "",
			InsulinType = InsulinTypePicker.SelectedItem as string,
			InsulinConcentration = ConcentrationPicker.SelectedItem as string ?? "U-40",
			WeightUnit = WeightUnitPicker.SelectedItem as string ?? "lbs"
		};

		if (double.TryParse(DoseEntry.Text, out var dose))
			pet.CurrentDoseIU = dose;

		if (double.TryParse(WeightEntry.Text, out var weight))
			pet.CurrentWeight = weight;

		await db.SavePetAsync(pet);
		_savedPetId = pet.Id;
	}

	private async Task SaveSchedulesAsync()
	{
		if (_savedPetId is null) return;
		var db = GetDatabaseService();

		if (MorningInsulinEnabled.IsToggled)
			await SaveScheduleAsync(db, "Morning Insulin", "Insulin",
				MorningInsulinTime.Time ?? new TimeSpan(7, 0, 0),
				int.TryParse(MorningInsulinReminder.Text, out var r1) ? r1 : 15);

		if (EveningInsulinEnabled.IsToggled)
			await SaveScheduleAsync(db, "Evening Insulin", "Insulin",
				EveningInsulinTime.Time ?? new TimeSpan(19, 0, 0),
				int.TryParse(EveningInsulinReminder.Text, out var r2) ? r2 : 15);

		if (MorningFeedingEnabled.IsToggled)
			await SaveScheduleAsync(db, "Morning Feeding", "Feeding",
				MorningFeedingTime.Time ?? new TimeSpan(7, 0, 0),
				int.TryParse(MorningFeedingReminder.Text, out var r3) ? r3 : 15);

		if (EveningFeedingEnabled.IsToggled)
			await SaveScheduleAsync(db, "Evening Feeding", "Feeding",
				EveningFeedingTime.Time ?? new TimeSpan(19, 0, 0),
				int.TryParse(EveningFeedingReminder.Text, out var r4) ? r4 : 15);
	}

	private async Task SaveScheduleAsync(IDatabaseService db, string label, string type,
		TimeSpan time, int reminderMinutes)
	{
		var schedule = new Schedule
		{
			PetId = _savedPetId!,
			Label = label,
			ScheduleType = type,
			TimeOfDay = time,
			ReminderLeadTimeMinutes = reminderMinutes,
			IsEnabled = true
		};
		await db.SaveScheduleAsync(schedule);
	}

	private async Task SaveVetInfoAsync()
	{
		if (_savedPetId is null) return;
		var vetName = VetNameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(vetName)) return;

		var db = GetDatabaseService();
		var vet = new VetInfo
		{
			PetId = _savedPetId,
			VetName = vetName,
			ClinicName = ClinicNameEntry.Text?.Trim() ?? "",
			Phone = VetPhoneEntry.Text?.Trim() ?? "",
			EmergencyPhone = EmergencyPhoneEntry.Text?.Trim() ?? "",
			Address = VetAddressEntry.Text?.Trim() ?? ""
		};
		await db.SaveVetInfoAsync(vet);
	}

	private static IDatabaseService GetDatabaseService() =>
		IPlatformApplication.Current!.Services.GetRequiredService<IDatabaseService>();

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
