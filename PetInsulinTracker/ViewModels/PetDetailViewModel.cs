using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class PetDetailViewModel : ObservableObject, IDisposable
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private readonly INotificationService _notifications;
	private CancellationTokenSource? _timerCts;
	private Task? _timerTask;
	private InsulinLog? _cachedLastInsulinLog;
	private FeedingLog? _cachedLastFeedingLog;

	public PetDetailViewModel(IDatabaseService db, ISyncService syncService, INotificationService notifications)
	{
		_db = db;
		_syncService = syncService;
		_notifications = notifications;
		WeakReferenceMessenger.Default.Register<WeightUnitChangedMessage>(this, (r, msg) =>
		{
			var vm = (PetDetailViewModel)r;
			if (vm.PetId is not null)
				Task.Run(() => vm.LoadDataAsync(vm.PetId));
		});
		WeakReferenceMessenger.Default.Register<PetSavedMessage>(this, (r, msg) =>
		{
			var vm = (PetDetailViewModel)r;
			if (vm.PetId is not null)
				Task.Run(() => vm.LoadDataAsync(vm.PetId));
		});
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private Pet? pet;

	[ObservableProperty]
	private InsulinLog? lastInsulinLog;

	[ObservableProperty]
	private WeightLog? lastWeightLog;

	[ObservableProperty]
	private string lastInsulinText = "No insulin logged yet";

	[ObservableProperty]
	private string lastWeightText = "No weight recorded";

	[ObservableProperty]
	private string lastFeedingText = "No feeding logged yet";

	[ObservableProperty]
	private double doseProgress;

	[ObservableProperty]
	private string doseCountdownText = "";

	[ObservableProperty]
	private string doseCountdownSubText = "";

	[ObservableProperty]
	private string feedingCountdownText = "";

	[ObservableProperty]
	private string feedingCountdownSubText = "";

	[ObservableProperty]
	private double feedingProgress;

	[ObservableProperty]
	private string doseInfoText = "";

	[ObservableProperty]
	private string quickFeedInfoText = "";

	[ObservableProperty]
	private bool hasCombinedSchedule;

	[ObservableProperty]
	private bool isDoseOverdue;

	[ObservableProperty]
	private bool isFeedingOverdue;

	[ObservableProperty]
	private Color doseRingColor = Colors.Transparent;

	[ObservableProperty]
	private Color feedingRingColor = Color.FromArgb("#4CAF50");

	[ObservableProperty]
	private bool isGuest;

	[ObservableProperty]
	private bool isOwnerOrFull;

	[ObservableProperty]
	private bool isOwner;

	[ObservableProperty]
	private ObservableCollection<Schedule> activeSchedules = [];

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private string syncStatus = "Not synced";

	[ObservableProperty]
	private bool showSeparateFeedingCountdown = true;

	private List<Schedule> _schedules = [];

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			_ = LoadDataAsync(value);
		}
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		if (PetId is not null)
			await LoadDataAsync(PetId);
	}

	private async Task LoadDataAsync(string id)
	{
		Pet = await _db.GetPetAsync(id);
		if (Pet is null) return;

		IsGuest = Pet.AccessLevel == "guest";
		IsOwnerOrFull = Pet.AccessLevel != "guest";
		IsOwner = Pet.AccessLevel == "owner";

		// Load last sync time for this pet
		var lastSyncStr = Preferences.Get($"lastSync_{id}", string.Empty);
		if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var lastSyncTime))
		{
			SyncStatus = $"Last synced: {lastSyncTime:g}";
		}
		else
		{
			SyncStatus = "Not synced";
		}

		DoseInfoText = Pet.CurrentDoseIU.HasValue
			? $"Dose: {Pet.CurrentDoseIU.Value} IU ({Pet.InsulinConcentration ?? "U-40"})"
			: "No dose set";

		QuickFeedInfoText = !string.IsNullOrEmpty(Pet.DefaultFoodName)
			? $"{Pet.DefaultFoodName} · {Pet.DefaultFoodAmount ?? 0} {Pet.DefaultFoodUnit}"
			: "Set food defaults in Edit Pet";

		var insulinLog = await _db.GetLatestInsulinLogAsync(id);
		LastInsulinLog = insulinLog;
		LastInsulinText = insulinLog is not null
			? $"{insulinLog.DoseIU} IU — {insulinLog.AdministeredAt:g}"
			: "No insulin logged yet";

		var weightLog = await _db.GetLatestWeightLogAsync(id);
		LastWeightLog = weightLog;
		LastWeightText = weightLog is not null
			? $"{weightLog.Weight} {weightLog.Unit} — {weightLog.RecordedAt:d}"
			: "No weight recorded";

		var feedingLogs = await _db.GetFeedingLogsAsync(id);
		var lastFeeding = feedingLogs.FirstOrDefault();
		LastFeedingText = lastFeeding is not null
			? $"{lastFeeding.FoodName} ({lastFeeding.Amount} {lastFeeding.Unit}) — {lastFeeding.FedAt:g}"
			: "No feeding logged yet";

		_schedules = await _db.GetSchedulesAsync(id);
		ActiveSchedules = new ObservableCollection<Schedule>(_schedules);
		HasCombinedSchedule = _schedules.Any(s => s.ScheduleType == Constants.ScheduleTypeCombined);

		// Cache logs for timer updates
		_cachedLastInsulinLog = insulinLog;
		_cachedLastFeedingLog = lastFeeding;

		// Dose countdown calculation
		UpdateDoseCountdown(insulinLog);

		// Feeding countdown calculation
		UpdateFeedingCountdown(lastFeeding);
	}

	private static DateTime? GetNextScheduledTimeAfter(List<Schedule> schedules, string scheduleType, DateTime referenceTime, out bool hasCombinedSchedule)
	{
		var matching = schedules
			.Where(s => s.ScheduleType == scheduleType || s.ScheduleType == Constants.ScheduleTypeCombined)
			.OrderBy(s => s.TimeOfDay)
			.ToList();
		
		// Check if any matching schedule is a combined type
		hasCombinedSchedule = matching.Any(s => s.ScheduleType == Constants.ScheduleTypeCombined);
		
		if (matching.Count == 0) return null;

		var today = referenceTime.Date;

		// Find the next upcoming time today
		foreach (var s in matching)
		{
			var candidate = today + s.TimeOfDay;
			if (candidate > referenceTime)
				return candidate;
		}

		// All times today have passed — use the first one tomorrow
		return today.AddDays(1) + matching[0].TimeOfDay;
	}

	private void UpdateDoseCountdown(InsulinLog? lastDose)
	{
		var referenceTime = DateTime.Now;
		var scheduledNext = GetNextScheduledTimeAfter(_schedules, "Insulin", referenceTime, out bool hasCombinedScheduleForInsulin);

		if (scheduledNext is not null && lastDose is not null)
		{
			var minutesFromLast = Math.Abs((scheduledNext.Value - lastDose.AdministeredAt).TotalMinutes);
			if (minutesFromLast <= Constants.ScheduleBufferMinutes)
			{
				referenceTime = scheduledNext.Value.AddMinutes(1);
				scheduledNext = GetNextScheduledTimeAfter(_schedules, "Insulin", referenceTime, out hasCombinedScheduleForInsulin);
			}
		}

		if (scheduledNext is not null)
		{
			var remaining = scheduledNext.Value - DateTime.Now;
			if (remaining.TotalMinutes <= 0)
			{
				IsDoseOverdue = true;
				DoseRingColor = Color.FromArgb("#D32F2F");
				DoseProgress = 1.0;
				DoseCountdownText = "OVERDUE";
				DoseCountdownSubText = "Dose is late";
			}
			else
			{
				IsDoseOverdue = false;
				DoseRingColor = Application.Current?.Resources.TryGetValue("CurrentPrimary", out var primary) == true && primary is Color c 
					? c 
					: Color.FromArgb("#FF6B9D");
				if (lastDose is not null)
				{
					var total = scheduledNext.Value - lastDose.AdministeredAt;
					DoseProgress = total.TotalMinutes > 0
						? Math.Clamp(1.0 - remaining.TotalMinutes / total.TotalMinutes, 0, 1)
						: 0;
				}
				else
				{
					DoseProgress = 0;
				}

				DoseCountdownText = remaining.TotalHours >= 1
					? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
					: $"{remaining.Minutes}m";
				var scheduleTimeText = scheduledNext.Value.ToString("t");
				DoseCountdownSubText = hasCombinedScheduleForInsulin 
					? $"Next dose & feeding at {scheduleTimeText}" 
					: $"Next dose at {scheduleTimeText}";
			}
		}
		else if (lastDose is not null)
		{
			// Fallback: 12-hour interval from last dose
			var intervalHours = 12.0;
			var elapsed = DateTime.Now - lastDose.AdministeredAt;
			var remaining = TimeSpan.FromHours(intervalHours) - elapsed;

			if (remaining.TotalMinutes <= 0)
			{
				IsDoseOverdue = true;
				DoseRingColor = Color.FromArgb("#D32F2F");
				DoseProgress = 1.0;
				DoseCountdownText = "OVERDUE";
				DoseCountdownSubText = "Dose is late";
			}
			else
			{
				IsDoseOverdue = false;
				DoseRingColor = Application.Current?.Resources.TryGetValue("CurrentPrimary", out var primary) == true && primary is Color c 
					? c 
					: Color.FromArgb("#FF6B9D");
				DoseProgress = Math.Clamp(elapsed.TotalHours / intervalHours, 0, 1);
				DoseCountdownText = remaining.TotalHours >= 1
					? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
					: $"{remaining.Minutes}m";
				var estimatedTime = DateTime.Now.Add(remaining);
				DoseCountdownSubText = $"Next dose ~{estimatedTime:t}";
			}
		}
		else
		{
			IsDoseOverdue = false;
			DoseRingColor = Application.Current?.Resources.TryGetValue("CurrentPrimary", out var primary) == true && primary is Color c 
				? c 
				: Color.FromArgb("#FF6B9D");
			DoseProgress = 0;
			DoseCountdownText = "--:--";
			DoseCountdownSubText = "No schedule set";
		}
	}

	private void UpdateFeedingCountdown(FeedingLog? lastFeeding)
	{
		var referenceTime = DateTime.Now;
		var scheduledNext = GetNextScheduledTimeAfter(_schedules, "Feeding", referenceTime, out bool hasCombinedScheduleForFeeding);

		if (scheduledNext is not null && lastFeeding is not null)
		{
			var minutesFromLast = Math.Abs((scheduledNext.Value - lastFeeding.FedAt).TotalMinutes);
			if (minutesFromLast <= Constants.ScheduleBufferMinutes)
			{
				referenceTime = scheduledNext.Value.AddMinutes(1);
				scheduledNext = GetNextScheduledTimeAfter(_schedules, "Feeding", referenceTime, out hasCombinedScheduleForFeeding);
			}
		}
		
		// If there's a combined schedule that handles feeding, don't show separate feeding countdown
		ShowSeparateFeedingCountdown = !hasCombinedScheduleForFeeding;

		if (scheduledNext is null)
		{
			IsFeedingOverdue = false;
			FeedingRingColor = Color.FromArgb("#4CAF50");
			FeedingProgress = 0;
			FeedingCountdownText = "--:--";
			FeedingCountdownSubText = "No schedule set";
			return;
		}

		var remaining = scheduledNext.Value - DateTime.Now;

		if (remaining.TotalMinutes <= 0)
		{
			IsFeedingOverdue = true;
			FeedingRingColor = Color.FromArgb("#D32F2F");
			FeedingProgress = 1.0;
			FeedingCountdownText = "OVERDUE";
			FeedingCountdownSubText = "Feeding is late";
		}
		else
		{
			IsFeedingOverdue = false;
			FeedingRingColor = Color.FromArgb("#4CAF50");
			if (lastFeeding is not null)
			{
				var total = scheduledNext.Value - lastFeeding.FedAt;
				FeedingProgress = total.TotalMinutes > 0
					? Math.Clamp(1.0 - remaining.TotalMinutes / total.TotalMinutes, 0, 1)
					: 0;
			}
			else
			{
				FeedingProgress = 0;
			}

			FeedingCountdownText = remaining.TotalHours >= 1
				? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
				: $"{remaining.Minutes}m";
			var scheduleTimeText = scheduledNext.Value.ToString("t");
			FeedingCountdownSubText = $"Next feeding at {scheduleTimeText}";
		}
	}

	[RelayCommand]
	private async Task QuickLogInsulinAsync()
	{
		if (Pet is null) return;

		var log = new InsulinLog
		{
			PetId = Pet.Id,
			DoseIU = Pet.CurrentDoseIU ?? 0,
			AdministeredAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};
		await _db.SaveInsulinLogAsync(log);
		_cachedLastInsulinLog = log;
		await SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	[RelayCommand]
	private async Task QuickLogFeedingAsync()
	{
		if (Pet is null) return;

		var log = new FeedingLog
		{
			PetId = Pet.Id,
			FoodName = Pet.DefaultFoodName ?? "Meal",
			Amount = Pet.DefaultFoodAmount ?? 0,
			Unit = Pet.DefaultFoodUnit,
			FoodType = Pet.DefaultFoodType,
			FedAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};
		await _db.SaveFeedingLogAsync(log);
		_cachedLastFeedingLog = log;
		await SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	[RelayCommand]
	private async Task QuickLogCombinedAsync()
	{
		if (Pet is null) return;

		var insulinLog = new InsulinLog
		{
			PetId = Pet.Id,
			DoseIU = Pet.CurrentDoseIU ?? 0,
			AdministeredAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		var feedingLog = new FeedingLog
		{
			PetId = Pet.Id,
			FoodName = Pet.DefaultFoodName ?? "Meal",
			Amount = Pet.DefaultFoodAmount ?? 0,
			Unit = Pet.DefaultFoodUnit,
			FoodType = Pet.DefaultFoodType,
			FedAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveInsulinLogAsync(insulinLog);
		await _db.SaveFeedingLogAsync(feedingLog);
		_cachedLastInsulinLog = insulinLog;
		_cachedLastFeedingLog = feedingLog;
		await SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	[RelayCommand]
	private async Task SyncNowAsync()
	{
		if (Pet is null) return;
		await SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	private async Task SyncInBackgroundAsync(string petId)
	{
		try
		{
			IsSyncing = true;
			SyncStatus = "Syncing…";
			await _syncService.SyncAsync(petId);
			// Read the timestamp saved by SyncService
			var lastSyncStr = Preferences.Get($"lastSync_{petId}", string.Empty);
			if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var lastSyncTime))
			{
				SyncStatus = $"Last synced: {lastSyncTime:g}";
				// Also update global sync time for Settings page
				Preferences.Set(Constants.LastSyncTimeKey, lastSyncStr);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
			SyncStatus = $"Sync failed: {ex.Message}";
		}
		finally
		{
			IsSyncing = false;
		}
	}

	[RelayCommand]
	private async Task GoToEditPetAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.AddEditPetPage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToInsulinLogAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.InsulinLogPage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToFeedingLogAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.FeedingLogPage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToWeightLogAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.WeightLogPage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToVetInfoAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.VetInfoPage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToScheduleAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.SchedulePage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private async Task GoToShareAsync()
	{
		if (Pet is null) return;
		await Shell.Current.GoToAsync($"{nameof(Views.SharePage)}?petId={Pet.Id}");
	}

	[RelayCommand]
	private static async Task GoToSettingsAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.SettingsPage));
	}

	[RelayCommand]
	private async Task LeavePetAsync()
	{
		if (Pet is null) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Leave Pet",
			$"Remove {Pet.Name} from your list? You can rejoin later with a new share code.",
			"Leave",
			"Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.LeavePetAsync(Pet.Id);
			await _notifications.CancelNotificationsForPetAsync(Pet.Id);
			await _db.PurgePetDataAsync(Pet.Id);
			Preferences.Remove($"lastSync_{Pet.Id}");
			Preferences.Remove(Constants.GetPetNotificationsKey(Pet.Id));
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	private async Task DeletePetAsync()
	{
		if (Pet is null) return;

		var confirm = await Shell.Current.DisplayAlertAsync(
			"Delete Pet",
			$"This permanently deletes {Pet.Name}, all data, and all share codes. This cannot be undone.",
			"Delete",
			"Cancel");

		if (!confirm) return;

		try
		{
			await _syncService.DeletePetAsync(Pet.Id);
			await _notifications.CancelNotificationsForPetAsync(Pet.Id);
			await _db.PurgePetDataAsync(Pet.Id);
			Preferences.Remove($"lastSync_{Pet.Id}");
			Preferences.Remove(Constants.GetPetNotificationsKey(Pet.Id));
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
		}
	}

	public void StartCountdownTimer()
	{
		// Cancel any existing timer
		StopCountdownTimer();

		_timerCts = new CancellationTokenSource();
		_timerTask = Task.Run(async () =>
		{
			while (!_timerCts.Token.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(30), _timerCts.Token);
					
					// Update countdowns on UI thread
					MainThread.BeginInvokeOnMainThread(() =>
					{
						UpdateDoseCountdown(_cachedLastInsulinLog);
						UpdateFeedingCountdown(_cachedLastFeedingLog);
					});
				}
				catch (TaskCanceledException)
				{
					break;
				}
			}
		}, _timerCts.Token);
	}

	public void StopCountdownTimer()
	{
		_timerCts?.Cancel();
		_timerCts?.Dispose();
		_timerCts = null;
		_timerTask = null;
	}

	public void Dispose()
	{
		StopCountdownTimer();
		WeakReferenceMessenger.Default.Unregister<WeightUnitChangedMessage>(this);
		WeakReferenceMessenger.Default.Unregister<PetSavedMessage>(this);
	}
}
