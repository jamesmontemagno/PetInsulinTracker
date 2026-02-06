using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class PetDetailPage : ContentPage
{
	public PetDetailPage(PetDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is PetDetailViewModel vm)
			vm.RefreshCommand.Execute(null);

		// Page entrance animation
		Content.Opacity = 0;
		Content.TranslationY = 20;
		await Task.WhenAll(
			Content.FadeToAsync(1, 300, Easing.CubicOut),
			Content.TranslateToAsync(0, 0, 300, Easing.CubicOut));
	}
}
