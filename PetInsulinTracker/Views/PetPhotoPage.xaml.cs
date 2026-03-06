using PetInsulinTracker.ViewModels;

namespace PetInsulinTracker.Views;

public partial class PetPhotoPage : ContentPage
{
	private double _currentScale = 1.0;
	private double _startScale = 1.0;

	public PetPhotoPage(PetPhotoViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
	{
		switch (e.Status)
		{
			case GestureStatus.Started:
				_startScale = _currentScale;
				PetPhotoImage.AnchorX = e.ScaleOrigin.X;
				PetPhotoImage.AnchorY = e.ScaleOrigin.Y;
				break;

			case GestureStatus.Running:
				_currentScale = Math.Max(1.0, Math.Min(_startScale * e.Scale, 5.0));
				PetPhotoImage.Scale = _currentScale;
				break;

			case GestureStatus.Completed:
				break;
		}
	}

	private void OnDoubleTapped(object sender, TappedEventArgs e)
	{
		if (_currentScale > 1.0)
		{
			_currentScale = 1.0;
			PetPhotoImage.ScaleTo(1.0, 250, Easing.CubicOut);
		}
		else
		{
			_currentScale = 2.5;
			PetPhotoImage.ScaleTo(2.5, 250, Easing.CubicOut);
		}
	}

	private async void OnCloseClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
