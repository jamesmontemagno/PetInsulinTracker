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
	private string doseRingSubText = "";

	[ObservableProperty]
	private string feedingCountdownText = "";

	[ObservableProperty]
	private string feedingCountdownSubText = "";

	[ObservableProperty]
	private string feedingRingSubText = "";

	[ObservableProperty]
	private double feedingProgress;

	[ObservableProperty]
	private string doseInfoText = "";

	[ObservableProperty]
	private string quickFeedInfoText = "";

	[ObservableProperty]
	private bool hasCombinedSchedule;

	[ObservableProperty]
	private bool isGuest;

	[ObservableProperty]
	private bool isOwnerOrFull;

	[ObservableProperty]
	private bool isOwner;

	[ObservableProperty]
	private ObservableCollection<Schedule> activeSchedules = [];

	[ObservableProperty]
	private ObservableCollection<Schedule> activeMedicationSchedules = [];

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private string syncStatus = "Not synced";

	[ObservableProperty]
	private bool showSeparateFeedingCountdown = true;

	[ObservableProperty]
	private bool isDoseOverdue;

	[ObservableProperty]
	private bool isFeedingOverdue;

	[ObservableProperty]
	private Color doseRingColor = Color.FromArgb("#5B9A6F");

	[ObservableProperty]
	private Color feedingRingColor = Color.FromArgb("#5B9A6F");

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

		// Restore last sync status from preferences
		var lastSyncStr = Preferences.Get($"lastSync_{id}", string.Empty);
		if (!string.IsNullOrEmpty(lastSyncStr) && DateTimeOffset.TryParse(lastSyncStr, out var lastSyncTime))
		{
			SyncStatus = $"Last synced: {lastSyncTime.LocalDateTime:g}";
		}
		else
		{
			SyncStatus = "Not synced";
		}

		IsGuest = Pet.AccessLevel == "guest";
		IsOwnerOrFull = Pet.AccessLevel != "guest";
		IsOwner = Pet.AccessLevel == "owner";

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
		ActiveSchedules = new ObservableCollection<Schedule>(
			_schedules.Where(s => s.ScheduleType != Constants.ScheduleTypeMedication));
		ActiveMedicationSchedules = new ObservableCollection<Schedule>(
			_schedules.Where(s => s.ScheduleType == Constants.ScheduleTypeMedication));
		HasCombinedSchedule = _schedules.Any(s => s.ScheduleType == Constants.ScheduleTypeCombined);

		// Cache logs for timer updates
		_cachedLastInsulinLog = insulinLog;
		_cachedLastFeedingLog = lastFeeding;

		// Dose countdown calculation
		UpdateDoseCountdown(insulinLog);

		// Feeding countdown calculation
		UpdateFeedingCountdown(lastFeeding);
	}

	/// <summary>Represents the schedule times surrounding "now" for countdown calculations.</summary>
	private sealed class ScheduleWindow(DateTime previous, DateTime next, DateTime firstToday)
	{
		public DateTime Previous { get; } = previous;
		public DateTime Next { get; } = next;
		public DateTime FirstToday { get; } = firstToday;
	}

	private enum CountdownState { Upcoming, DoneEarly, Overdue }

	/// <summary>
	/// Builds a window of previous/next/afterNext schedule occurrences for a given type.
	/// Returns false when no schedules match.
	/// </summary>
	private static bool TryGetScheduleWindow(
		List<Schedule> schedules, string scheduleType, DateTime now,
		out ScheduleWindow window, out bool hasCombinedSchedule)
	{
		window = null!;
		var matching = scheduleType == Constants.ScheduleTypeMedication
			? schedules.Where(s => s.ScheduleType == Constants.ScheduleTypeMedication)
			: schedules.Where(s => s.ScheduleType == scheduleType || s.ScheduleType == Constants.ScheduleTypeCombined);

		var matchingList = matching.ToList();
		hasCombinedSchedule = matchingList.Any(s => s.ScheduleType == Constants.ScheduleTypeCombined);

		var times = matchingList
			.Select(s => s.TimeOfDay)
			.Distinct()
			.OrderBy(t => t)
			.ToList();

		if (times.Count == 0) return false;

		var today = now.Date;
		var firstToday = today + times[0];

		var nextIndex = times.FindIndex(t => today + t > now);
		var nextDayOffset = 0;
		if (nextIndex == -1) { nextIndex = 0; nextDayOffset = 1; }
		var next = today.AddDays(nextDayOffset) + times[nextIndex];

		var prevIndex = times.FindLastIndex(t => today + t <= now);
		var prevDayOffset = 0;
		if (prevIndex == -1) { prevIndex = times.Count - 1; prevDayOffset = -1; }
		var previous = today.AddDays(prevDayOffset) + times[prevIndex];

		window = new ScheduleWindow(previous, next, firstToday);
		return true;
	}

	private static bool IsWithinEarlyWindow(DateTime loggedAt, DateTime scheduledAt)
		=> loggedAt < scheduledAt && loggedAt >= scheduledAt.AddMinutes(-Constants.ScheduleBufferMinutes);

	private static bool SatisfiesOccurrence(DateTime loggedAt, DateTime scheduledAt)
		=> loggedAt >= scheduledAt.AddMinutes(-Constants.ScheduleBufferMinutes);

	/// <summary>
	/// Determines countdown state given a schedule window and the most recent log timestamp.
	/// </summary>
	private static CountdownState GetCountdownState(ScheduleWindow window, DateTime? lastLogTime, DateTime now, out DateTime dueTime)
	{
		// Early completion: log within buffer before next, and we haven't reached next yet
		if (lastLogTime is not null && now < window.Next && IsWithinEarlyWindow(lastLogTime.Value, window.Next))
		{
			dueTime = window.Next;
			return CountdownState.DoneEarly;
		}

		// Before first schedule today — just count down, nothing overdue yet
		if (now < window.FirstToday)
		{
			dueTime = window.Next;
			return CountdownState.Upcoming;
		}

		// Was the previous occurrence satisfied?
		if (lastLogTime is not null && SatisfiesOccurrence(lastLogTime.Value, window.Previous))
		{
			// Linger the "done" state for the buffer period after the scheduled time
			if ((now - window.Previous).TotalMinutes <= Constants.ScheduleBufferMinutes)
			{
				dueTime = window.Next;
				return CountdownState.DoneEarly;
			}

			dueTime = window.Next;
			return CountdownState.Upcoming;
		}

		dueTime = window.Previous;
		return CountdownState.Overdue;
	}

	private static string FormatCountdown(TimeSpan remaining)
		=> remaining.TotalHours >= 1
			? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
			: remaining.TotalMinutes < 1 ? "<1m"
			: $"{remaining.Minutes}m";

	private void UpdateDoseCountdown(InsulinLog? lastDose)
	{
		var now = DateTime.Now;

		if (TryGetScheduleWindow(_schedules, Constants.ScheduleTypeInsulin, now, out var window, out bool isCombined))
		{
			var state = GetCountdownState(window, lastDose?.AdministeredAt, now, out var dueTime);
			var label = isCombined ? "dose + feed" : "dose";

			switch (state)
			{
				case CountdownState.DoneEarly:
					IsDoseOverdue = false;
					DoseProgress = 1.0;
					DoseCountdownText = "Done";
					DoseCountdownSubText = $"Done for {window.Next:t}";
					DoseRingSubText = $"{label} done";
					break;

				case CountdownState.Overdue:
					IsDoseOverdue = true;
					DoseProgress = 1.0;
					DoseCountdownText = "NOW";
					DoseCountdownSubText = "Dose due";
					DoseRingSubText = isCombined ? "dose + feed due" : "dose due";
					break;

				default: // Upcoming
					IsDoseOverdue = false;
					var remaining = dueTime - now;
					DoseRingSubText = isCombined ? "next dose + feed" : "next dose";
					DoseProgress = lastDose is not null && lastDose.AdministeredAt < dueTime
						? Math.Clamp(1.0 - remaining.TotalMinutes / (dueTime - lastDose.AdministeredAt).TotalMinutes, 0, 1)
						: 0;
					DoseCountdownText = FormatCountdown(remaining);
					var timeText = dueTime.ToString("t");
					DoseCountdownSubText = isCombined
						? $"Next dose & feeding at {timeText}"
						: $"Next dose at {timeText}";
					break;
			}
		}
		else if (lastDose is not null)
		{
			// Fallback: 12-hour interval from last dose
			var intervalHours = 12.0;
			var elapsed = now - lastDose.AdministeredAt;
			var remaining = TimeSpan.FromHours(intervalHours) - elapsed;

			if (remaining.TotalMinutes <= 0)
			{
				DoseProgress = 1.0;
				DoseCountdownText = "NOW";
				DoseCountdownSubText = "Dose due";
				DoseRingSubText = "dose due";
				IsDoseOverdue = true;
			}
			else
			{
				IsDoseOverdue = false;
				DoseRingSubText = "next dose";
				DoseProgress = Math.Clamp(elapsed.TotalHours / intervalHours, 0, 1);
				DoseCountdownText = FormatCountdown(remaining);
				DoseCountdownSubText = $"Next dose ~{now.Add(remaining):t}";
			}
		}
		else
		{
			IsDoseOverdue = false;
			DoseProgress = 0;
			DoseCountdownText = "--:--";
			DoseCountdownSubText = "No schedule set";
			DoseRingSubText = "dose";
		}

		DoseRingColor = IsDoseOverdue
			? Color.FromArgb("#D32F2F")
			: Color.FromArgb("#5B9A6F");
	}

	private void UpdateFeedingCountdown(FeedingLog? lastFeeding)
	{
		var now = DateTime.Now;

		if (TryGetScheduleWindow(_schedules, Constants.ScheduleTypeFeeding, now, out var window, out bool isCombined))
		{
			ShowSeparateFeedingCountdown = !isCombined;
			var state = GetCountdownState(window, lastFeeding?.FedAt, now, out var dueTime);

			switch (state)
			{
				case CountdownState.DoneEarly:
					IsFeedingOverdue = false;
					FeedingProgress = 1.0;
					FeedingCountdownText = "Done";
					FeedingCountdownSubText = $"Done for {window.Next:t}";
					FeedingRingSubText = "feeding done";
					break;

				case CountdownState.Overdue:
					IsFeedingOverdue = true;
					FeedingProgress = 1.0;
					FeedingCountdownText = "NOW";
					FeedingCountdownSubText = "Feeding due";
					FeedingRingSubText = "feed due";
					break;

				default: // Upcoming
					IsFeedingOverdue = false;
					var remaining = dueTime - now;
					FeedingRingSubText = "next feed";
					FeedingProgress = lastFeeding is not null && lastFeeding.FedAt < dueTime
						? Math.Clamp(1.0 - remaining.TotalMinutes / (dueTime - lastFeeding.FedAt).TotalMinutes, 0, 1)
						: 0;
					FeedingCountdownText = FormatCountdown(remaining);
					FeedingCountdownSubText = $"Next feeding at {dueTime:t}";
					break;
			}
		}
		else
		{
			ShowSeparateFeedingCountdown = true;
			IsFeedingOverdue = false;
			FeedingProgress = 0;
			FeedingCountdownText = "--:--";
			FeedingCountdownSubText = "No schedule set";
			FeedingRingSubText = "feeding";
			FeedingRingColor = Color.FromArgb("#5B9A6F");
			return;
		}

		FeedingRingColor = IsFeedingOverdue
			? Color.FromArgb("#D32F2F")
			: Color.FromArgb("#5B9A6F");
	}

	[RelayCommand]
	private async Task QuickLogInsulinAsync()
	{
		if (Pet is null) return;

		var insulinDose = Pet.CurrentDoseIU ?? 0;
		var insulinConcentration = Pet.InsulinConcentration ?? "U-40";
		var confirmInsulin = await Shell.Current.DisplayAlertAsync(
			"Confirm Quick Log",
			$"Log insulin now for {Pet.Name}?\n\n• Insulin: {insulinDose} IU ({insulinConcentration})",
			"Log",
			"Cancel");

		if (!confirmInsulin) return;

		var log = new InsulinLog
		{
			PetId = Pet.Id,
			DoseIU = insulinDose,
			AdministeredAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};
		await _db.SaveInsulinLogAsync(log);
		_ = SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	[RelayCommand]
	private async Task QuickLogFeedingAsync()
	{
		if (Pet is null) return;

		var foodName = string.IsNullOrWhiteSpace(Pet.DefaultFoodName) ? "Meal" : Pet.DefaultFoodName;
		var foodAmount = Pet.DefaultFoodAmount ?? 0;
		var foodUnit = string.IsNullOrWhiteSpace(Pet.DefaultFoodUnit) ? "portion" : Pet.DefaultFoodUnit;
		var confirmFeeding = await Shell.Current.DisplayAlertAsync(
			"Confirm Quick Log",
			$"Log feeding now for {Pet.Name}?\n\n• Food: {foodName}\n• Amount: {foodAmount} {foodUnit}",
			"Log",
			"Cancel");

		if (!confirmFeeding) return;

		var log = new FeedingLog
		{
			PetId = Pet.Id,
			FoodName = foodName,
			Amount = foodAmount,
			Unit = foodUnit,
			FoodType = Pet.DefaultFoodType,
			FedAt = DateTime.Now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};
		await _db.SaveFeedingLogAsync(log);
		_ = SyncInBackgroundAsync(Pet.Id);
		await LoadDataAsync(Pet.Id);
	}

	[RelayCommand]
	private async Task QuickLogCombinedAsync()
	{
		if (Pet is null) return;

		var insulinDose = Pet.CurrentDoseIU ?? 0;
		var insulinConcentration = Pet.InsulinConcentration ?? "U-40";
		var foodName = string.IsNullOrWhiteSpace(Pet.DefaultFoodName) ? "Meal" : Pet.DefaultFoodName;
		var foodAmount = Pet.DefaultFoodAmount ?? 0;
		var foodUnit = string.IsNullOrWhiteSpace(Pet.DefaultFoodUnit) ? "portion" : Pet.DefaultFoodUnit;

		var confirmCombined = await Shell.Current.DisplayAlertAsync(
			"Confirm Quick Log",
			$"Log feeding + insulin now for {Pet.Name}?\n\n• Insulin: {insulinDose} IU ({insulinConcentration})\n• Food: {foodName}\n• Amount: {foodAmount} {foodUnit}",
			"Log",
			"Cancel");

		if (!confirmCombined) return;

		var now = DateTime.Now;

		var insulinLog = new InsulinLog
		{
			PetId = Pet.Id,
			DoseIU = insulinDose,
			AdministeredAt = now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		var feedingLog = new FeedingLog
		{
			PetId = Pet.Id,
			FoodName = foodName,
			Amount = foodAmount,
			Unit = foodUnit,
			FoodType = Pet.DefaultFoodType,
			FedAt = now,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveInsulinLogAsync(insulinLog);
		await _db.SaveFeedingLogAsync(feedingLog);
		_ = SyncInBackgroundAsync(Pet.Id);
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
			SyncStatus = $"Last synced: {DateTime.Now:g}";
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
	private async Task GoToMedicationScheduleAsync()
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
