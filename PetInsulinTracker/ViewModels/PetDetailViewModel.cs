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

		// Dose countdown calculation
		UpdateDoseCountdown(insulinLog);
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

		// Assume 12-hour dosing interval by default
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
