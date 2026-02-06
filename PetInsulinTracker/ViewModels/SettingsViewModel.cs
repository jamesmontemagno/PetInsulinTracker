using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISyncService _syncService;

	public SettingsViewModel(ISyncService syncService)
	{
		_syncService = syncService;
		selectedTheme = Themes.ThemeService.CurrentTheme;
	}

	[ObservableProperty]
	private string ownerName = Preferences.Get(Constants.OwnerNameKey, string.Empty);

	[ObservableProperty]
	private bool notificationsEnabled = Preferences.Get("notifications_enabled", true);

	[ObservableProperty]
	private string weightUnit = Preferences.Get("default_weight_unit", "lbs");

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private string syncStatus = "Not synced";

	[ObservableProperty]
	private Themes.AppTheme selectedTheme;

	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];

	partial void OnOwnerNameChanged(string value)
	{
		Preferences.Set(Constants.OwnerNameKey, value);
	}

	partial void OnNotificationsEnabledChanged(bool value)
	{
		Preferences.Set("notifications_enabled", value);
	}

	partial void OnWeightUnitChanged(string value)
	{
		Preferences.Set("default_weight_unit", value);
	}

	partial void OnSelectedThemeChanged(Themes.AppTheme value)
	{
		Themes.ThemeService.ApplyTheme(value);
	}

	[RelayCommand]
	private void SelectTheme(string themeName)
	{
		if (Enum.TryParse<Themes.AppTheme>(themeName, out var theme))
		{
			SelectedTheme = theme;
		}
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
