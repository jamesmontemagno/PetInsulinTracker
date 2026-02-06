using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class PetDetailPage : ContentPage
{
	public PetDetailPage(PetDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PetDetailViewModel vm)
			vm.RefreshCommand.Execute(null);
	}
}
