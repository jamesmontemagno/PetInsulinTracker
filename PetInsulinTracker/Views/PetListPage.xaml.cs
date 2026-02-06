using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class PetListPage : ContentPage
{
	public PetListPage(PetListViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PetListViewModel vm)
			vm.LoadPetsCommand.Execute(null);
	}
}
