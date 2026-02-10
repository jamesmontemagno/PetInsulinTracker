using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class AddInsulinLogPage : ContentPage
{
	public AddInsulinLogPage(InsulinLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
