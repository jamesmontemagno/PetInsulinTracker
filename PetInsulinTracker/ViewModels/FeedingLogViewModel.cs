using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using PetInsulinTracker.Views;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class FeedingLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private List<FeedingLog> _allLogs = [];

	public FeedingLogViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<FeedingLog> logs = [];

	[ObservableProperty]
	private ObservableCollection<LogWeekGroup<FeedingLog>> groupedLogs = [];

	[ObservableProperty]
	private bool showingAll;

	[ObservableProperty]
	private bool isRefreshing;

	// New log entry fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveAndCloseCommand))]
	private string foodName = string.Empty;

	[ObservableProperty]
	private double amount;

	[ObservableProperty]
	private string unit = "cups";

	[ObservableProperty]
	private string foodType = "Dry";

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private TimeSpan logTime = DateTime.Now.TimeOfDay;

	[ObservableProperty]
	private string? notes;

	[ObservableProperty]
	private bool isSyncing;

	public List<string> UnitOptions { get; } = ["cups", "grams", "oz", "cans"];
	public List<string> FoodTypeOptions { get; } = ["Dry", "Wet", "Treat"];

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadDataAsync();
	}

	private async Task LoadDataAsync()
	{
		if (PetId is null) return;

		var pet = await _db.GetPetAsync(PetId);
		if (pet is not null)
		{
			if (!string.IsNullOrEmpty(pet.DefaultFoodName))
				FoodName = pet.DefaultFoodName;
			if (pet.DefaultFoodAmount is > 0)
				Amount = pet.DefaultFoodAmount.Value;
			if (!string.IsNullOrEmpty(pet.DefaultFoodUnit))
				Unit = pet.DefaultFoodUnit;
			if (!string.IsNullOrEmpty(pet.DefaultFoodType))
				FoodType = pet.DefaultFoodType;
		}

		await LoadLogsAsync();
	}

	[RelayCommand]
	private async Task LoadLogsAsync()
	{
		if (PetId is null) return;
		var logList = await _db.GetFeedingLogsAsync(PetId);

		// Filter for guest access — only show own logs
		var pet = await _db.GetPetAsync(PetId);
		if (pet?.AccessLevel == "guest")
			logList = logList.Where(l => l.LoggedById == Constants.DeviceUserId).ToList();

		_allLogs = logList;
		Logs = new ObservableCollection<FeedingLog>(logList);
		RebuildGroupedLogs();
		IsRefreshing = false;
	}

	private void RebuildGroupedLogs()
	{
		var groups = LogWeekGroup<FeedingLog>.GroupByWeek(
			_allLogs, l => l.FedAt, recentOnly: !ShowingAll);
		GroupedLogs = new ObservableCollection<LogWeekGroup<FeedingLog>>(groups);
	}

	[RelayCommand]
	private async Task ToggleShowAllAsync()
	{
		ShowingAll = !ShowingAll;
		RebuildGroupedLogs();
		await Task.CompletedTask;
	}

	[RelayCommand(CanExecute = nameof(CanSaveLog))]
	private async Task SaveLogAsync()
	{
		if (PetId is null) return;

		var log = new FeedingLog
		{
			PetId = PetId,
			FoodName = FoodName,
			Amount = Amount,
			Unit = Unit,
			FoodType = FoodType,
			FedAt = LogDate.Date + LogTime,
			Notes = Notes,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveFeedingLogAsync(log);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		// Reset form to pet defaults
		Notes = null;
		LogDate = DateTime.Today;
		LogTime = DateTime.Now.TimeOfDay;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => !string.IsNullOrWhiteSpace(FoodName);

	[RelayCommand(CanExecute = nameof(CanSaveLog))]
	private async Task SaveAndCloseAsync()
	{
		await SaveLogAsync();
		await Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task OpenAddLogAsync()
	{
		if (string.IsNullOrEmpty(PetId)) return;
		await Shell.Current.GoToAsync($"{nameof(AddFeedingLogPage)}?petId={PetId}");
	}

	[RelayCommand]
	private async Task CloseModalAsync()
	{
		await Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task DeleteLogAsync(FeedingLog log)
	{
		await _db.DeleteFeedingLogAsync(log);
		Logs.Remove(log);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);
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
}
