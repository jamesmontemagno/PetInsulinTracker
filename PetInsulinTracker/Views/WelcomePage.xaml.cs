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

		// Start on Berry Bliss (index 3)
		ThemeCarousel.Position = 3;
	}

	private void OnThemeCarouselChanged(object? sender, CurrentItemChangedEventArgs e)
	{
		if (e.CurrentItem is ThemePreview preview)
		{
			// Update border highlights
			foreach (var t in _themes)
				t.BorderColor = Colors.Transparent;
			preview.BorderColor = Color.FromArgb(preview.PrimaryHex);

			ThemeService.ApplyTheme(preview.Theme);
		}
	}

	private void OnNameCompleted(object? sender, EventArgs e) => SaveAndContinue();

	private void OnGetStartedClicked(object? sender, EventArgs e) => SaveAndContinue();

	private async void SaveAndContinue()
	{
		var name = NameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlertAsync("Name Required", "Please enter your name to continue.", "OK");
			return;
		}

		Preferences.Set(Constants.OwnerNameKey, name);
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
