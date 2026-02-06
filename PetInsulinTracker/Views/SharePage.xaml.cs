using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class SharePage : ContentPage
{
	public SharePage(ShareViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
