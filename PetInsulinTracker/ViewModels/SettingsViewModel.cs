using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISyncService _syncService;

	public SettingsViewModel(ISyncService syncService)
	{
		_syncService = syncService;
	}

	[ObservableProperty]
	private bool notificationsEnabled = Preferences.Get("notifications_enabled", true);

	[ObservableProperty]
	private string weightUnit = Preferences.Get("default_weight_unit", "lbs");

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private string syncStatus = "Not synced";

	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];

	partial void OnNotificationsEnabledChanged(bool value)
	{
		Preferences.Set("notifications_enabled", value);
	}

	partial void OnWeightUnitChanged(string value)
	{
		Preferences.Set("default_weight_unit", value);
	}

	[RelayCommand]
	private async Task SyncNowAsync()
	{
		try
		{
			IsSyncing = true;
			SyncStatus = "Syncing...";
			await _syncService.SyncAllAsync();
			SyncStatus = $"Last synced: {DateTime.Now:g}";
		}
		catch (Exception ex)
		{
			SyncStatus = $"Sync failed: {ex.Message}";
		}
		finally
		{
			IsSyncing = false;
		}
	}
}
