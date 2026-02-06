using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class WeightLogPage : ContentPage
{
	public WeightLogPage(WeightLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
