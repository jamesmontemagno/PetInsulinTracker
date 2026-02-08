using PetInsulinTracker.Views;

namespace PetInsulinTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(PetDetailPage), typeof(PetDetailPage));
		Routing.RegisterRoute(nameof(AddEditPetPage), typeof(AddEditPetPage));
		Routing.RegisterRoute(nameof(InsulinLogPage), typeof(InsulinLogPage));
		Routing.RegisterRoute(nameof(FeedingLogPage), typeof(FeedingLogPage));
		Routing.RegisterRoute(nameof(WeightLogPage), typeof(WeightLogPage));
		Routing.RegisterRoute(nameof(VetInfoPage), typeof(VetInfoPage));
		Routing.RegisterRoute(nameof(SchedulePage), typeof(SchedulePage));
		Routing.RegisterRoute(nameof(SharePage), typeof(SharePage));
		Routing.RegisterRoute(nameof(ImportPetPage), typeof(ImportPetPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
