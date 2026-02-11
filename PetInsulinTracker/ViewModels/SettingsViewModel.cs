using System.Collections.ObjectModel;
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
	private bool _suppressPetNotificationsChange;

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

		var lastSyncStr = Preferences.Get(Constants.LastSyncTimeKey, string.Empty);
		if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var lastSyncTime))
		{
			SyncStatus = $"Last synced: {lastSyncTime:g}";
		}
		else
		{
			SyncStatus = "Not synced";
		}
	}

	[ObservableProperty]
	private string ownerName = Preferences.Get(Constants.OwnerNameKey, string.Empty);

	[ObservableProperty]
	private bool notificationsEnabled = Preferences.Get(Constants.NotificationsEnabledKey, true);

	public bool ArePetNotificationsEnabled => NotificationsEnabled;
	public bool ShowPerPetNotificationsHint => !NotificationsEnabled;

	public ObservableCollection<PetNotificationItem> PetNotifications { get; } = [];

	[ObservableProperty]
	private bool offlineMode = Preferences.Get(Constants.OfflineModeKey, false);

	[ObservableProperty]
	private bool preferLocalImage = Preferences.Get(Constants.PreferLocalImageKey, true);

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

		OnPropertyChanged(nameof(ArePetNotificationsEnabled));
		OnPropertyChanged(nameof(ShowPerPetNotificationsHint));

		_ = HandleNotificationsChangedAsync(value);
	}

	public async Task LoadPetNotificationsAsync()
	{
		var pets = await _db.GetPetsAsync();
		_suppressPetNotificationsChange = true;
		PetNotifications.Clear();
		foreach (var pet in pets.OrderBy(p => p.Name))
		{
			var enabled = Preferences.Get(Constants.GetPetNotificationsKey(pet.Id), true);
			PetNotifications.Add(new PetNotificationItem(pet.Id, pet.Name, enabled, HandlePetNotificationChangedAsync));
		}
		_suppressPetNotificationsChange = false;
	}

	private async Task HandlePetNotificationChangedAsync(PetNotificationItem item, bool isEnabled)
	{
		if (_suppressPetNotificationsChange)
			return;

		var key = Constants.GetPetNotificationsKey(item.PetId);
		Preferences.Set(key, isEnabled);

		if (!NotificationsEnabled)
			return;

		if (isEnabled)
		{
			var granted = await _notifications.EnsurePermissionAsync();
			if (!granted)
			{
				_suppressPetNotificationsChange = true;
				item.IsEnabled = false;
				_suppressPetNotificationsChange = false;
				Preferences.Set(key, false);
				return;
			}

			await _notifications.ScheduleNotificationsForPetAsync(item.PetId);
		}
		else
		{
			await _notifications.CancelNotificationsForPetAsync(item.PetId);
		}
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

	partial void OnPreferLocalImageChanged(bool value)
	{
		Preferences.Set(Constants.PreferLocalImageKey, value);
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
			var now = DateTime.Now;
			Preferences.Set(Constants.LastSyncTimeKey, now.ToString("O"));
			SyncStatus = $"Last synced: {now:g}";
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

public sealed class PetNotificationItem : ObservableObject
{
	private readonly Func<PetNotificationItem, bool, Task> _onToggled;
	private bool _isEnabled;

	public PetNotificationItem(string petId, string petName, bool isEnabled, Func<PetNotificationItem, bool, Task> onToggled)
	{
		PetId = petId;
		PetName = petName;
		_isEnabled = isEnabled;
		_onToggled = onToggled;
	}

	public string PetId { get; }
	public string PetName { get; }

	public bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (SetProperty(ref _isEnabled, value))
				_ = _onToggled(this, value);
		}
	}
}
