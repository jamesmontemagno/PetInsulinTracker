using PetInsulinTracker.Themes;
using PetInsulinTracker.Views;

namespace PetInsulinTracker;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Apply saved theme on startup
		ThemeService.ApplyTheme(ThemeService.CurrentTheme);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var setupComplete = Preferences.Get("setup_complete", false);
		return new Window(setupComplete ? new AppShell() : new WelcomePage());
	}
}