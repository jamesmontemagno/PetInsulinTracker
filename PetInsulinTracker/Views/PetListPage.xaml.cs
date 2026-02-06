using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class PetListPage : ContentPage
{
	public PetListPage(PetListViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PetListViewModel vm)
			vm.LoadPetsCommand.Execute(null);

		// Page entrance animation
		Content.Opacity = 0;
		Content.TranslationY = 20;
		await Task.WhenAll(
			Content.FadeToAsync(1, 300, Easing.CubicOut),
			Content.TranslateToAsync(0, 0, 300, Easing.CubicOut));
	}
}
