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
	private string? fullAccessCode;

	[ObservableProperty]
	private string? guestAccessCode;

	[ObservableProperty]
	private bool isGenerating;

	[ObservableProperty]
	private bool isLoadingUsers;

	[ObservableProperty]
	private string statusMessage = string.Empty;

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
		FullAccessCode = Pet?.FullAccessCode;
		GuestAccessCode = Pet?.GuestAccessCode;
		OnPropertyChanged(nameof(IsOwner));

		if (IsOwner && (!string.IsNullOrEmpty(FullAccessCode) || !string.IsNullOrEmpty(GuestAccessCode)))
			await LoadAllSharedUsersAsync();
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

		// Check if a code already exists for this access level
		var existingCode = accessLevel == "guest" ? GuestAccessCode : FullAccessCode;
		if (!string.IsNullOrEmpty(existingCode))
		{
			var label = accessLevel == "guest" ? "Guest Access" : "Full Access";
			var confirm = await Shell.Current.DisplayAlertAsync(
				"Replace Share Code?",
				$"You already have a {label} code ({existingCode}). Generating a new one will replace it. Anyone using the old code will need the new one. Continue?",
				"Generate New", "Keep Existing");

			if (!confirm) return;
		}

		try
		{
			IsGenerating = true;
			StatusMessage = "Generating share code...";

			var code = await _syncService.GenerateShareCodeAsync(Pet.Id, accessLevel);

			if (accessLevel == "guest")
			{
				GuestAccessCode = code;
				Pet.GuestAccessCode = code;
				OnPropertyChanged(nameof(GuestAccessCode));
			}
			else
			{
				FullAccessCode = code;
				Pet.FullAccessCode = code;
				OnPropertyChanged(nameof(FullAccessCode));
			}

			// Set the primary ShareCode to the most recently generated one
			ShareCode = code;
			Pet.ShareCode = code;
			await _db.SavePetAsync(Pet);

			// Sync pet data to the server so the code is redeemable
			try
			{
				await _syncService.SyncAsync(Pet.Id);
			}
			catch
			{
				// Sync failure shouldn't block code generation; it will retry on next sync
			}

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
	private async Task CopyShareCodeAsync()
	{
		if (string.IsNullOrEmpty(ShareCode)) return;
		await Clipboard.Default.SetTextAsync(ShareCode);
		StatusMessage = "Share code copied to clipboard!";
	}

	[RelayCommand]
	private async Task CopyFullAccessCodeAsync()
	{
		if (string.IsNullOrEmpty(FullAccessCode)) return;
		await Clipboard.Default.SetTextAsync(FullAccessCode);
		StatusMessage = "Full access code copied to clipboard!";
	}

	[RelayCommand]
	private async Task CopyGuestAccessCodeAsync()
	{
		if (string.IsNullOrEmpty(GuestAccessCode)) return;
		await Clipboard.Default.SetTextAsync(GuestAccessCode);
		StatusMessage = "Guest access code copied to clipboard!";
	}

	[RelayCommand]
	private async Task LoadSharedUsersAsync()
	{
		await LoadAllSharedUsersAsync();
	}

	private async Task LoadAllSharedUsersAsync()
	{
		try
		{
			IsLoadingUsers = true;
			SharedUsers.Clear();

			if (Pet is not null)
			{
				var users = await _syncService.GetSharedUsersAsync(Pet.Id);
				foreach (var user in users)
					SharedUsers.Add(user);
			}
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
		if (user is null || Pet is null) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Revoke Access",
			$"Are you sure you want to revoke access for {user.DisplayName}? They will no longer be able to sync this pet's data.",
			"Revoke", "Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.RevokeAccessAsync(Pet.Id, user.DeviceUserId);
			StatusMessage = $"Access revoked for {user.DisplayName}.";
			await LoadAllSharedUsersAsync();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
	}

	[RelayCommand]
	private async Task DeleteFullAccessCodeAsync()
	{
		if (Pet is null || string.IsNullOrEmpty(FullAccessCode)) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Delete Share Code?",
			$"This will permanently deactivate the Full Access code ({FullAccessCode}). Anyone using it will no longer be able to redeem or sync. Continue?",
			"Delete", "Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.DeleteShareCodeAsync(FullAccessCode);
			Pet.FullAccessCode = null;
			if (Pet.ShareCode == FullAccessCode)
			{
				Pet.ShareCode = GuestAccessCode;
				ShareCode = GuestAccessCode;
			}
			FullAccessCode = null;
			await _db.SavePetAsync(Pet);
			StatusMessage = "Full access code deleted.";
			await LoadAllSharedUsersAsync();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
	}

	[RelayCommand]
	private async Task DeleteGuestAccessCodeAsync()
	{
		if (Pet is null || string.IsNullOrEmpty(GuestAccessCode)) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Delete Share Code?",
			$"This will permanently deactivate the Guest Access code ({GuestAccessCode}). Anyone using it will no longer be able to redeem or sync. Continue?",
			"Delete", "Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.DeleteShareCodeAsync(GuestAccessCode);
			Pet.GuestAccessCode = null;
			if (Pet.ShareCode == GuestAccessCode)
			{
				Pet.ShareCode = FullAccessCode;
				ShareCode = FullAccessCode;
			}
			GuestAccessCode = null;
			await _db.SavePetAsync(Pet);
			StatusMessage = "Guest access code deleted.";
			await LoadAllSharedUsersAsync();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error: {ex.Message}";
		}
	}
}
