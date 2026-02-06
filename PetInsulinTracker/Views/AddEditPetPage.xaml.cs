using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class AddEditPetPage : ContentPage
{
	public AddEditPetPage(AddEditPetViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
