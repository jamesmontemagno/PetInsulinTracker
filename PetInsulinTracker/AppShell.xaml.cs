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
		Routing.RegisterRoute(nameof(AddInsulinLogPage), typeof(AddInsulinLogPage));
		Routing.RegisterRoute(nameof(FeedingLogPage), typeof(FeedingLogPage));
		Routing.RegisterRoute(nameof(AddFeedingLogPage), typeof(AddFeedingLogPage));
		Routing.RegisterRoute(nameof(WeightLogPage), typeof(WeightLogPage));
		Routing.RegisterRoute(nameof(AddWeightLogPage), typeof(AddWeightLogPage));
		Routing.RegisterRoute(nameof(VetInfoPage), typeof(VetInfoPage));
		Routing.RegisterRoute(nameof(SchedulePage), typeof(SchedulePage));
		Routing.RegisterRoute(nameof(MedicationLogPage), typeof(MedicationLogPage));
		Routing.RegisterRoute(nameof(AddMedicationLogPage), typeof(AddMedicationLogPage));
		Routing.RegisterRoute(nameof(SharePage), typeof(SharePage));
		Routing.RegisterRoute(nameof(ImportPetPage), typeof(ImportPetPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
