using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using PetInsulinTracker.Views;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class MedicationLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private const int DefaultLimit = 50;
	private const string OtherMedicationOption = "Other";
	private bool _isUpdatingMedicationNameFromPicker;
	private bool _isMedicationNamePrefilled;

	public MedicationLogViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<MedicationLog> logs = [];

	[ObservableProperty]
	private ObservableCollection<LogWeekGroup<MedicationLog>> groupedLogs = [];

	[ObservableProperty]
	private bool showingAll;

	[ObservableProperty]
	private bool isRefreshing;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveAndCloseCommand))]
	private string? medicationName = string.Empty;

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private TimeSpan logTime = DateTime.Now.TimeOfDay;

	[ObservableProperty]
	private string? notes;

	[ObservableProperty]
	private bool isSyncing;

	[ObservableProperty]
	private ObservableCollection<string> medicationPickerOptions = [];

	[ObservableProperty]
	private string? selectedMedicationOption;

	[ObservableProperty]
	private bool showMedicationPicker;

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadLogsAsync();
	}

	partial void OnMedicationNameChanged(string? value)
	{
		if (!_isUpdatingMedicationNameFromPicker)
			_isMedicationNamePrefilled = false;
	}

	[RelayCommand]
	private async Task LoadLogsAsync()
	{
		if (PetId is null) return;

		var schedules = await _db.GetSchedulesAsync(PetId);
		var medSchedules = schedules
			.Where(s => s.ScheduleType == Constants.ScheduleTypeMedication)
			.OrderBy(s => s.TimeOfDay)
			.ToList();

		var pickerOptions = medSchedules
			.Select(s => s.Label)
			.Where(label => !string.IsNullOrWhiteSpace(label))
			.Distinct()
			.ToList();

		ShowMedicationPicker = pickerOptions.Count > 0;

		if (ShowMedicationPicker)
			pickerOptions.Add(OtherMedicationOption);

		MedicationPickerOptions = new ObservableCollection<string>(pickerOptions);

		// Pre-fill with upcoming medication schedule if form is empty
		if (string.IsNullOrWhiteSpace(MedicationName) && pickerOptions.Count > 0)
		{
			var now = DateTime.Now;
			var today = now.Date;

			// Find the next upcoming medication schedule
			var next = medSchedules.FirstOrDefault(s => today + s.TimeOfDay > now)
				?? medSchedules[0]; // Fallback to first if all passed today

			if (!string.IsNullOrWhiteSpace(next.Label))
				SelectedMedicationOption = next.Label;
		}

		var pet = await _db.GetPetAsync(PetId);
		var logList = ShowingAll
			? await _db.GetMedicationLogsAsync(PetId)
			: await _db.GetMedicationLogsAsync(PetId, DefaultLimit);

		// Filter for guest access — only show own logs
		if (pet?.AccessLevel == "guest")
			logList = logList.Where(l => l.LoggedById == Constants.DeviceUserId).ToList();

		Logs = new ObservableCollection<MedicationLog>(logList);
		GroupedLogs = new ObservableCollection<LogWeekGroup<MedicationLog>>(
			LogWeekGroup<MedicationLog>.GroupByWeek(logList, l => l.AdministeredAt, recentOnly: !ShowingAll));
		IsRefreshing = false;
	}

	partial void OnSelectedMedicationOptionChanged(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return;

		if (value == OtherMedicationOption)
		{
			if (string.IsNullOrWhiteSpace(MedicationName) || _isMedicationNamePrefilled)
				SetMedicationNameFromPicker(string.Empty);

			_isMedicationNamePrefilled = false;

			return;
		}

		SetMedicationNameFromPicker(value);
		_isMedicationNamePrefilled = true;
	}

	private void SetMedicationNameFromPicker(string? value)
	{
		_isUpdatingMedicationNameFromPicker = true;
		MedicationName = value;
		_isUpdatingMedicationNameFromPicker = false;
	}

	[RelayCommand]
	private async Task ToggleShowAllAsync()
	{
		ShowingAll = !ShowingAll;
		await LoadLogsAsync();
	}

	[RelayCommand(CanExecute = nameof(CanSaveLog))]
	private async Task SaveLogAsync()
	{
		if (PetId is null) return;

		var log = new MedicationLog
		{
			PetId = PetId,
			MedicationName = MedicationName ?? string.Empty,
			AdministeredAt = LogDate.Date + LogTime,
			Notes = Notes,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveMedicationLogAsync(log);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		// Reset form (keep medication name for repeat logging)
		Notes = null;
		LogDate = DateTime.Today;
		LogTime = DateTime.Now.TimeOfDay;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => !string.IsNullOrWhiteSpace(MedicationName);

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
		await Shell.Current.GoToAsync($"{nameof(AddMedicationLogPage)}?petId={PetId}");
	}

	[RelayCommand]
	private async Task CloseModalAsync()
	{
		await Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task DeleteLogAsync(MedicationLog log)
	{
		await _db.DeleteMedicationLogAsync(log);
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
