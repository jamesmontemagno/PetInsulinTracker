using PetInsulinTracker.Services;
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

		// Re-apply when system light/dark changes so resolved keys stay correct
		RequestedThemeChanged += (_, _) => ThemeService.ApplyTheme(ThemeService.CurrentTheme);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var setupComplete = Preferences.Get("setup_complete", false);
		return new Window(setupComplete ? new AppShell() : new WelcomePage());
	}

	protected override void OnResume()
	{
		base.OnResume();
		_ = Task.Run(async () =>
		{
			try
			{
				var syncService = IPlatformApplication.Current?.Services.GetService<ISyncService>();
				if (syncService is not null)
					await syncService.SyncAllAsync();
			}
			catch
			{
				// Silently fail for offline scenarios
			}
		});
	}
}