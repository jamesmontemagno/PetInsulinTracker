using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class AddMedicationLogPage : ContentPage
{
	public AddMedicationLogPage(MedicationLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
