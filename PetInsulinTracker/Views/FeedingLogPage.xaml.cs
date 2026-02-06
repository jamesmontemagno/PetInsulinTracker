using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class FeedingLogPage : ContentPage
{
	public FeedingLogPage(FeedingLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		Content.Opacity = 0;
		Content.TranslationY = 20;
		await Task.WhenAll(
			Content.FadeToAsync(1, 300, Easing.CubicOut),
			Content.TranslateToAsync(0, 0, 300, Easing.CubicOut));
	}
}
