using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly ISyncService _syncService;
	private readonly IDatabaseService _db;
	private readonly INotificationService _notifications;
	private bool _suppressNotificationsChange;

	public SettingsViewModel(ISyncService syncService, IDatabaseService db, INotificationService notifications)
	{
		_syncService = syncService;
		_db = db;
		_notifications = notifications;
		selectedThemeName = Themes.ThemeService.CurrentTheme switch
		{
			Themes.AppTheme.Warm => "Warm & Earthy",
			Themes.AppTheme.Ocean => "Ocean Breeze",
			Themes.AppTheme.Forest => "Forest Walk",
			Themes.AppTheme.Midnight => "Midnight Indigo",
			_ => "Berry Bliss"
		};
	}

	[ObservableProperty]
	private string ownerName = Preferences.Get(Constants.OwnerNameKey, string.Empty);

	[ObservableProperty]
	private bool notificationsEnabled = Preferences.Get(Constants.NotificationsEnabledKey, true);

	[ObservableProperty]
	private bool offlineMode = Preferences.Get(Constants.OfflineModeKey, false);

	[ObservableProperty]
	private string weightUnit = Preferences.Get("default_weight_unit", "lbs");

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private string syncStatus = "Not synced";

	[ObservableProperty]
	private string selectedThemeName;

	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];

	public List<string> ThemeDisplayNames { get; } = ["Warm & Earthy", "Ocean Breeze", "Forest Walk", "Berry Bliss", "Midnight Indigo"];

	partial void OnOwnerNameChanged(string value)
	{
		Preferences.Set(Constants.OwnerNameKey, value);
	}

	partial void OnNotificationsEnabledChanged(bool value)
	{
		if (_suppressNotificationsChange)
			return;

		_ = HandleNotificationsChangedAsync(value);
	}

	private async Task HandleNotificationsChangedAsync(bool value)
	{
		if (value)
		{
			var granted = await _notifications.EnsurePermissionAsync();
			if (!granted)
			{
				_suppressNotificationsChange = true;
				NotificationsEnabled = false;
				_suppressNotificationsChange = false;
				Preferences.Set(Constants.NotificationsEnabledKey, false);
				return;
			}

			Preferences.Set(Constants.NotificationsEnabledKey, true);
			await _notifications.RescheduleAllAsync();
		}
		else
		{
			Preferences.Set(Constants.NotificationsEnabledKey, false);
			await _notifications.CancelAllAsync();
		}
	}

	partial void OnOfflineModeChanged(bool value)
	{
		Preferences.Set(Constants.OfflineModeKey, value);
		OnPropertyChanged(nameof(IsSyncVisible));
	}

	public bool IsSyncVisible => !OfflineMode;

	partial void OnWeightUnitChanged(string value)
	{
		Preferences.Set("default_weight_unit", value);
		_ = UpdateAllPetsWeightUnitAsync(value);
	}

	private async Task UpdateAllPetsWeightUnitAsync(string newUnit)
	{
		var pets = await _db.GetPetsAsync();
		foreach (var pet in pets)
		{
			var oldUnit = pet.WeightUnit;
			if (pet.CurrentWeight.HasValue && !string.Equals(oldUnit, newUnit, StringComparison.OrdinalIgnoreCase))
				pet.CurrentWeight = WeightConverter.Convert(pet.CurrentWeight.Value, oldUnit, newUnit);
			pet.WeightUnit = newUnit;
			await _db.SavePetAsync(pet);

			var logs = await _db.GetWeightLogsAsync(pet.Id);
			foreach (var log in logs)
			{
				if (!string.Equals(log.Unit, newUnit, StringComparison.OrdinalIgnoreCase))
					log.Weight = WeightConverter.Convert(log.Weight, log.Unit, newUnit);
				log.Unit = newUnit;
				await _db.SaveWeightLogAsync(log);
			}
		}
		WeakReferenceMessenger.Default.Send(new WeightUnitChangedMessage(newUnit));
	}

	partial void OnSelectedThemeNameChanged(string value)
	{
		var theme = value switch
		{
			"Ocean Breeze" => Themes.AppTheme.Ocean,
			"Forest Walk" => Themes.AppTheme.Forest,
			"Berry Bliss" => Themes.AppTheme.Berry,
			"Midnight Indigo" => Themes.AppTheme.Midnight,
			_ => Themes.AppTheme.Warm
		};
		Themes.ThemeService.ApplyTheme(theme);
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
