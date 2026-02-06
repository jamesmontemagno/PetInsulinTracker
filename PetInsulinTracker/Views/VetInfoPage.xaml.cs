using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class VetInfoPage : ContentPage
{
	public VetInfoPage(VetInfoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
