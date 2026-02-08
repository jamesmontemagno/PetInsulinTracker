using MauiIcons.Core;
using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class VetInfoPage : ContentPage
{
	public VetInfoPage(VetInfoViewModel viewModel)
	{
		InitializeComponent();
		_ = new MauiIcon(); // Workaround for MauiIcons XAML compilation
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
