using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using PetInsulinTracker.Views;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class InsulinLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private List<InsulinLog> _allLogs = [];

	public InsulinLogViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<InsulinLog> logs = [];

	[ObservableProperty]
	private ObservableCollection<LogWeekGroup<InsulinLog>> groupedLogs = [];

	[ObservableProperty]
	private bool showingAll;

	[ObservableProperty]
	private bool isRefreshing;

	// New log entry fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveAndCloseCommand))]
	private double doseIU;

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private TimeSpan logTime = DateTime.Now.TimeOfDay;

	[ObservableProperty]
	private string? injectionSite;

	[ObservableProperty]
	private string? notes;

	[ObservableProperty]
	private bool isSyncing;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadLogsAsync();
	}

	[RelayCommand]
	private async Task LoadLogsAsync()
	{
		if (PetId is null) return;

		var pet = await _db.GetPetAsync(PetId);
		if (pet?.CurrentDoseIU is not null && DoseIU == 0)
			DoseIU = pet.CurrentDoseIU.Value;

		var logList = await _db.GetInsulinLogsAsync(PetId);

		// Filter for guest access — only show own logs
		if (pet?.AccessLevel == "guest")
			logList = logList.Where(l => l.LoggedById == Constants.DeviceUserId).ToList();

		_allLogs = logList;
		Logs = new ObservableCollection<InsulinLog>(logList);
		RebuildGroupedLogs();
		IsRefreshing = false;
	}

	private void RebuildGroupedLogs()
	{
		var groups = LogWeekGroup<InsulinLog>.GroupByWeek(
			_allLogs, l => l.AdministeredAt, recentOnly: !ShowingAll);
		GroupedLogs = new ObservableCollection<LogWeekGroup<InsulinLog>>(groups);
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

		var log = new InsulinLog
		{
			PetId = PetId,
			DoseIU = DoseIU,
			AdministeredAt = LogDate.Date + LogTime,
			InjectionSite = InjectionSite,
			Notes = Notes,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveInsulinLogAsync(log);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		// Reset form
		InjectionSite = null;
		Notes = null;
		LogDate = DateTime.Today;
		LogTime = DateTime.Now.TimeOfDay;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => DoseIU > 0;

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
		await Shell.Current.GoToAsync($"{nameof(AddInsulinLogPage)}?petId={PetId}");
	}

	[RelayCommand]
	private async Task CloseModalAsync()
	{
		await Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task DeleteLogAsync(InsulinLog log)
	{
		await _db.DeleteInsulinLogAsync(log);
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
