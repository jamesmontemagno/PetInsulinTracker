using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class AddWeightLogPage : ContentPage
{
	public AddWeightLogPage(WeightLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
