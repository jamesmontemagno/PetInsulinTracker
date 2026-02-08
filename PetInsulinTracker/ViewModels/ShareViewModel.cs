using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using PetInsulinTracker.Shared.DTOs;

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
	private bool isLoadingUsers;

	[ObservableProperty]
	private string statusMessage = string.Empty;

	[ObservableProperty]
	private string shareCodeLabel = "Share Code";

	public ObservableCollection<SharedUserDto> SharedUsers { get; } = [];

	public bool IsOwner => Pet?.AccessLevel == "owner";

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadPetAsync(value);
	}

	private async Task LoadPetAsync(string id)
	{
		Pet = await _db.GetPetAsync(id);
		ShareCode = Pet?.ShareCode;
		OnPropertyChanged(nameof(IsOwner));

		if (IsOwner && !string.IsNullOrEmpty(ShareCode))
			await LoadSharedUsersAsync();
	}

	[RelayCommand]
	private async Task GenerateFullAccessCodeAsync()
	{
		await GenerateCodeAsync("full");
	}

	[RelayCommand]
	private async Task GenerateGuestAccessCodeAsync()
	{
		await GenerateCodeAsync("guest");
	}

	private async Task GenerateCodeAsync(string accessLevel)
	{
		if (Pet is null) return;

		try
		{
			IsGenerating = true;
			StatusMessage = "Generating share code...";

			var code = await _syncService.GenerateShareCodeAsync(Pet.Id, accessLevel);
			ShareCode = code;
			ShareCodeLabel = accessLevel == "guest" ? "Guest Access Code" : "Full Access Code";
			Pet.ShareCode = code;
			await _db.SavePetAsync(Pet);

			StatusMessage = accessLevel == "guest"
				? "Guest code generated! They can view pet info and log entries, but won't see your logs."
				: "Full access code generated! They can see all logs and pet info.";
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

	[RelayCommand]
	private async Task LoadSharedUsersAsync()
	{
		if (string.IsNullOrEmpty(ShareCode)) return;

		try
		{
			IsLoadingUsers = true;
			var users = await _syncService.GetSharedUsersAsync(ShareCode);
			SharedUsers.Clear();
			foreach (var user in users)
				SharedUsers.Add(user);
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error loading shared users: {ex.Message}";
		}
		finally
		{
			IsLoadingUsers = false;
		}
	}

	[RelayCommand]
	private async Task RevokeAccessAsync(SharedUserDto user)
	{
		if (string.IsNullOrEmpty(ShareCode) || user is null) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Revoke Access",
			$"Are you sure you want to revoke access for {user.DisplayName}? They will no longer be able to sync this pet's data.",
			"Revoke", "Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.RevokeAccessAsync(ShareCode, user.DeviceUserId);
			StatusMessage = $"Access revoked for {user.DisplayName}.";
			await LoadSharedUsersAsync();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
	}
}
