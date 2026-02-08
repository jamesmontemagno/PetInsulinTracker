using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public partial class ImportPetViewModel : ObservableObject
{
	private readonly ISyncService _syncService;

	public ImportPetViewModel(ISyncService syncService)
	{
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? redeemCode;

	[ObservableProperty]
	private bool isRedeeming;

	[ObservableProperty]
	private string statusMessage = string.Empty;

	[RelayCommand]
	private async Task RedeemShareCodeAsync()
	{
		if (string.IsNullOrWhiteSpace(RedeemCode)) return;

		try
		{
			IsRedeeming = true;
			StatusMessage = "Importing pet data...";

			await _syncService.RedeemShareCodeAsync(RedeemCode.Trim().ToUpperInvariant());
			StatusMessage = "Pet imported successfully!";
			RedeemCode = null;

			// Navigate back after a brief delay so the user sees the success message
			await Task.Delay(1000);
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
		finally
		{
			IsRedeeming = false;
		}
	}
}
