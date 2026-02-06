using PetInsulinTracker.Helpers;

namespace PetInsulinTracker.Views;

public partial class WelcomePage : ContentPage
{
	public WelcomePage()
	{
		InitializeComponent();
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
