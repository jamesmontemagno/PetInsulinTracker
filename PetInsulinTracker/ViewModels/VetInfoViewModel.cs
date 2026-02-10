using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class VetInfoViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private VetInfo? _existingInfo;

	public VetInfoViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private string vetName = string.Empty;

	[ObservableProperty]
	private string clinicName = string.Empty;

	[ObservableProperty]
	private string? phone;

	[ObservableProperty]
	private string? emergencyPhone;

	[ObservableProperty]
	private string? address;

	[ObservableProperty]
	private string? email;

	[ObservableProperty]
	private string? vetNotes;

	[ObservableProperty]
	private bool isSyncing;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadVetInfoAsync();
	}

	[RelayCommand]
	private async Task LoadVetInfoAsync()
	{
		if (PetId is null) return;

		_existingInfo = await _db.GetVetInfoAsync(PetId);
		if (_existingInfo is null) return;

		VetName = _existingInfo.VetName;
		ClinicName = _existingInfo.ClinicName;
		Phone = _existingInfo.Phone;
		EmergencyPhone = _existingInfo.EmergencyPhone;
		Address = _existingInfo.Address;
		Email = _existingInfo.Email;
		VetNotes = _existingInfo.Notes;
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		if (PetId is null) return;

		var info = _existingInfo ?? new VetInfo { PetId = PetId };
		info.VetName = VetName;
		info.ClinicName = ClinicName;
		info.Phone = Phone;
		info.EmergencyPhone = EmergencyPhone;
		info.Address = Address;
		info.Email = Email;
		info.Notes = VetNotes;

		await _db.SaveVetInfoAsync(info);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		await Shell.Current.GoToAsync("..");
	}

	private async Task SyncInBackgroundAsync(string petId)
	{
		try
		{
			IsSyncing = true;
			await _syncService.SyncAsync(petId);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
		}
		finally
		{
			IsSyncing = false;
		}
	}

	[RelayCommand]
	private async Task CallVetAsync()
	{
		if (string.IsNullOrEmpty(Phone)) return;
		try
		{
			PhoneDialer.Default.Open(Phone);
		}
		catch
		{
			await Shell.Current.DisplayAlertAsync("Error", "Unable to make phone call", "OK");
		}
	}

	[RelayCommand]
	private async Task CallEmergencyAsync()
	{
		if (string.IsNullOrEmpty(EmergencyPhone)) return;
		try
		{
			PhoneDialer.Default.Open(EmergencyPhone);
		}
		catch
		{
			await Shell.Current.DisplayAlertAsync("Error", "Unable to make phone call", "OK");
		}
	}
}
