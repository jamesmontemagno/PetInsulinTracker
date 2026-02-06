using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class FeedingLogPage : ContentPage
{
	public FeedingLogPage(FeedingLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
