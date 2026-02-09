using System.Collections.ObjectModel;
using System.Linq;
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
	private const string FullAccessLevel = "full";

	private CancellationTokenSource? _statusCts;

	public ShareViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
		FullAccessCodes.CollectionChanged += (_, _) => UpdateCodeFlags();
		GuestAccessCodes.CollectionChanged += (_, _) => UpdateCodeFlags();
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private Pet? pet;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanCreateShareCode))]
	private bool isGenerating;

	[ObservableProperty]
	private bool isLoadingUsers;

	[ObservableProperty]
	private bool isLoadingCodes;

	[ObservableProperty]
	private string statusMessage = string.Empty;

	public ObservableCollection<ShareCodeDto> FullAccessCodes { get; } = [];
	public ObservableCollection<ShareCodeDto> GuestAccessCodes { get; } = [];

	public ObservableCollection<SharedUserDto> SharedUsers { get; } = [];

	public bool IsOwner => Pet?.AccessLevel == "owner";
	public bool IsOwnerOrFull => Pet?.AccessLevel != "guest";
	public bool CanCreateShareCode => IsOwnerOrFull && !IsGenerating;
	public bool HasFullAccessCodes => FullAccessCodes.Count > 0;
	public bool HasGuestAccessCodes => GuestAccessCodes.Count > 0;
	public bool HasAnyCodes => HasFullAccessCodes || HasGuestAccessCodes;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadPetAsync(value);
	}

	private async Task LoadPetAsync(string id)
	{
		try
		{
			Pet = await _db.GetPetAsync(id);
			OnPropertyChanged(nameof(IsOwner));
			OnPropertyChanged(nameof(IsOwnerOrFull));
			OnPropertyChanged(nameof(CanCreateShareCode));

			if (!IsOwnerOrFull)
			{
				SetStatus("Share management requires owner or full access.", 0);
				await Shell.Current.GoToAsync("..");
				return;
			}

			await LoadShareCodesAsync();
			await LoadAllSharedUsersAsync();
		}
		catch (Exception ex)
		{
			SetStatus($"Error loading pet: {ex.Message}");
		}
	}

	[RelayCommand]
	private async Task CreateShareCodeAsync()
	{
		if (Pet is null || !IsOwnerOrFull) return;

		var selection = await Shell.Current.DisplayActionSheetAsync(
			"Create Share Code",
			"Cancel",
			null,
			"Full Access",
			"Guest Access");

		if (string.IsNullOrEmpty(selection) || selection == "Cancel") return;

		var accessLevel = selection == "Guest Access" ? "guest" : FullAccessLevel;
		var existingCount = accessLevel == "guest" ? GuestAccessCodes.Count : FullAccessCodes.Count;
		if (existingCount > 0)
		{
			var confirm = await Shell.Current.DisplayAlertAsync(
				"Create Another Share Code?",
				$"You already have {existingCount} {selection} code{(existingCount == 1 ? "" : "s")}. Create another?",
				"Create Another",
				"Cancel");
			if (!confirm) return;
		}

		await GenerateCodeAsync(accessLevel);
	}

	private async Task GenerateCodeAsync(string accessLevel)
	{
		if (Pet is null) return;

		try
		{
			IsGenerating = true;
			SetStatus("Generating share code...", 0);

			await _syncService.GenerateShareCodeAsync(Pet.Id, accessLevel);
			await LoadShareCodesAsync();

			SetStatus(accessLevel == "guest"
				? "Guest share code created."
				: "Full access share code created.");
		}
		catch (Exception ex)
		{
			SetStatus($"Error: {ex.Message}");
		}
		finally
		{
			IsGenerating = false;
		}
	}

	[RelayCommand]
	private async Task CopyShareCodeAsync(ShareCodeDto code)
	{
		if (string.IsNullOrEmpty(code?.Code)) return;
		await Clipboard.Default.SetTextAsync(code.Code);
		SetStatus("Share code copied to clipboard!");
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
			SetStatus($"Error loading shared users: {ex.Message}");
		}
		finally
		{
			IsLoadingUsers = false;
		}
	}

	private async Task LoadShareCodesAsync()
	{
		if (Pet is null) return;
		try
		{
			IsLoadingCodes = true;
			var codes = await _syncService.GetShareCodesAsync(Pet.Id);
			UpdateShareCodes(codes);
		}
		catch (Exception ex)
		{
			SetStatus($"Error loading share codes: {ex.Message}");
		}
		finally
		{
			IsLoadingCodes = false;
		}
	}

	private void UpdateShareCodes(List<ShareCodeDto> codes)
	{
		FullAccessCodes.Clear();
		GuestAccessCodes.Clear();

		foreach (var code in codes.OrderByDescending(c => c.CreatedAt))
		{
			if (code.AccessLevel == "guest")
				GuestAccessCodes.Add(code);
			else
				FullAccessCodes.Add(code);
		}
		UpdateCodeFlags();
	}

	private void UpdateCodeFlags()
	{
		OnPropertyChanged(nameof(HasFullAccessCodes));
		OnPropertyChanged(nameof(HasGuestAccessCodes));
		OnPropertyChanged(nameof(HasAnyCodes));
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
			SetStatus($"Access revoked for {user.DisplayName}.");
			await LoadAllSharedUsersAsync();
		}
		catch (Exception ex)
		{
			SetStatus($"Error: {ex.Message}");
		}
	}

	[RelayCommand]
	private async Task DeleteShareCodeAsync(ShareCodeDto code)
	{
		if (Pet is null || !IsOwnerOrFull || string.IsNullOrEmpty(code?.Code)) return;
		var label = code.AccessLevel == "guest" ? "Guest Access" : "Full Access";

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Delete Share Code?",
			$"This will permanently deactivate the {label} code ({code.Code}). It will only prevent future redemptions — anyone who already redeemed keeps access. Continue?",
			"Delete", "Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.DeleteShareCodeAsync(code.Code);
			SetStatus("Share code deleted.");
			await LoadShareCodesAsync();
		}
		catch (Exception ex)
		{
			SetStatus($"Error: {ex.Message}");
		}
	}

	[RelayCommand]
	private Task RefreshShareCodesAsync() => LoadShareCodesAsync();

	private void SetStatus(string message, int autoHideMs = 5000)
	{
		_statusCts?.Cancel();
		StatusMessage = message;
		if (autoHideMs > 0 && !string.IsNullOrEmpty(message))
		{
			var cts = _statusCts = new CancellationTokenSource();
			_ = ClearStatusAfterDelayAsync(autoHideMs, cts.Token);
		}
	}

	private async Task ClearStatusAfterDelayAsync(int delayMs, CancellationToken ct)
	{
		try
		{
			await Task.Delay(delayMs, ct);
			StatusMessage = string.Empty;
		}
		catch (TaskCanceledException) { }
	}
}
