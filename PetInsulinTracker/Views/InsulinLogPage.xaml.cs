using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class InsulinLogPage : ContentPage
{
	public InsulinLogPage(InsulinLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
