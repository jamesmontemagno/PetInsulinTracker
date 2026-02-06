using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class PetDetailViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public PetDetailViewModel(IDatabaseService db)
	{
		_db = db;
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
	private ObservableCollection<Schedule> activeSchedules = [];

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
		ActiveSchedules = new ObservableCollection<Schedule>(_schedules.Where(s => s.IsEnabled));

		// Dose countdown calculation
		UpdateDoseCountdown(insulinLog);

		// Feeding countdown calculation
		UpdateFeedingCountdown(lastFeeding);
	}

	private static DateTime? GetNextScheduledTime(List<Schedule> schedules, string scheduleType)
	{
		var enabled = schedules
			.Where(s => s.ScheduleType == scheduleType && s.IsEnabled)
			.OrderBy(s => s.TimeOfDay)
			.ToList();
		if (enabled.Count == 0) return null;

		var now = DateTime.Now;
		var today = now.Date;

		// Find the next upcoming time today
		foreach (var s in enabled)
		{
			var candidate = today + s.TimeOfDay;
			if (candidate > now)
				return candidate;
		}

		// All times today have passed — use the first one tomorrow
		return today.AddDays(1) + enabled[0].TimeOfDay;
	}

	private void UpdateDoseCountdown(InsulinLog? lastDose)
	{
		if (lastDose is null || Pet is null)
		{
			DoseProgress = 0;
			DoseCountdownText = "--:--";
			DoseCountdownSubText = "No dose logged";
			return;
		}

		var scheduledNext = GetNextScheduledTime(_schedules, "Insulin");

		if (scheduledNext is not null)
		{
			var remaining = scheduledNext.Value - DateTime.Now;
			if (remaining.TotalMinutes <= 0)
			{
				DoseProgress = 1.0;
				DoseCountdownText = "NOW";
				DoseCountdownSubText = "Dose due";
			}
			else
			{
				// Progress based on time since last dose toward the scheduled time
				var total = scheduledNext.Value - lastDose.AdministeredAt;
				DoseProgress = total.TotalMinutes > 0
					? Math.Clamp(1.0 - remaining.TotalMinutes / total.TotalMinutes, 0, 1)
					: 0;
				DoseCountdownText = remaining.TotalHours >= 1
					? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
					: $"{remaining.Minutes}m";
				DoseCountdownSubText = "Until next dose";
			}
		}
		else
		{
			// Fallback: 12-hour interval from last dose
			var intervalHours = 12.0;
			var elapsed = DateTime.Now - lastDose.AdministeredAt;
			var remaining = TimeSpan.FromHours(intervalHours) - elapsed;

			if (remaining.TotalMinutes <= 0)
			{
				DoseProgress = 1.0;
				DoseCountdownText = "NOW";
				DoseCountdownSubText = "Dose due";
			}
			else
			{
				DoseProgress = Math.Clamp(elapsed.TotalHours / intervalHours, 0, 1);
				DoseCountdownText = remaining.TotalHours >= 1
					? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
					: $"{remaining.Minutes}m";
				DoseCountdownSubText = "Until next dose";
			}
		}
	}

	private void UpdateFeedingCountdown(FeedingLog? lastFeeding)
	{
		var scheduledNext = GetNextScheduledTime(_schedules, "Feeding");

		if (scheduledNext is null)
		{
			FeedingProgress = 0;
			FeedingCountdownText = "--:--";
			FeedingCountdownSubText = "No schedule set";
			return;
		}

		var remaining = scheduledNext.Value - DateTime.Now;

		if (remaining.TotalMinutes <= 0)
		{
			FeedingProgress = 1.0;
			FeedingCountdownText = "NOW";
			FeedingCountdownSubText = "Feeding due";
		}
		else
		{
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
			FeedingCountdownSubText = "Until next feeding";
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
			LoggedBy = Constants.OwnerName
		};
		await _db.SaveInsulinLogAsync(log);
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
			LoggedBy = Constants.OwnerName
		};
		await _db.SaveFeedingLogAsync(log);
		await LoadDataAsync(Pet.Id);
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
}
