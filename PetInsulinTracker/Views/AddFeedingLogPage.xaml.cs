using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class AddFeedingLogPage : ContentPage
{
	public AddFeedingLogPage(FeedingLogViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
