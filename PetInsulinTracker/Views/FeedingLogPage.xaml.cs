using MauiIcons.Core;
using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class FeedingLogPage : ContentPage
{
	public FeedingLogPage(FeedingLogViewModel viewModel)
	{
		InitializeComponent();
		_ = new MauiIcon(); // Workaround for MauiIcons XAML compilation
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var loadTask = BindingContext is FeedingLogViewModel viewModel
			? viewModel.LoadLogsCommand.ExecuteAsync(null)
			: Task.CompletedTask;
		Content.Opacity = 0;
		Content.TranslationY = 20;
		await Task.WhenAll(
			loadTask,
			Content.FadeToAsync(1, 300, Easing.CubicOut),
			Content.TranslateToAsync(0, 0, 300, Easing.CubicOut));
	}
}
