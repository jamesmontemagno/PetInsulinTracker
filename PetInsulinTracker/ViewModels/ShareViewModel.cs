using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class ShareViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;

	public ShareViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private Pet? pet;

	[ObservableProperty]
	private string? shareCode;

	[ObservableProperty]
	private string? redeemCode;

	[ObservableProperty]
	private bool isGenerating;

	[ObservableProperty]
	private bool isRedeeming;

	[ObservableProperty]
	private string statusMessage = string.Empty;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadPetAsync(value);
	}

	private async Task LoadPetAsync(string id)
	{
		Pet = await _db.GetPetAsync(id);
		ShareCode = Pet?.ShareCode;
	}

	[RelayCommand]
	private async Task GenerateShareCodeAsync()
	{
		if (Pet is null) return;

		try
		{
			IsGenerating = true;
			StatusMessage = "Generating share code...";

			var code = await _syncService.GenerateShareCodeAsync(Pet.Id);
			ShareCode = code;
			Pet.ShareCode = code;
			await _db.SavePetAsync(Pet);

			StatusMessage = "Share code generated! Share this code with your family or pet sitter.";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
		finally
		{
			IsGenerating = false;
		}
	}

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

	[RelayCommand]
	private async Task CopyShareCodeAsync()
	{
		if (string.IsNullOrEmpty(ShareCode)) return;
		await Clipboard.Default.SetTextAsync(ShareCode);
		StatusMessage = "Share code copied to clipboard!";
	}
}
