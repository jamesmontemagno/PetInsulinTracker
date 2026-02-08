using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class ScheduleViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;

	public ScheduleViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<Schedule> schedules = [];

	// New schedule fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveScheduleCommand))]
	private string label = string.Empty;

	[ObservableProperty]
	private string scheduleType = "Insulin";

	[ObservableProperty]
	private TimeSpan timeOfDay = new(8, 0, 0);

	[ObservableProperty]
	private int reminderLeadTimeMinutes = 15;

	[ObservableProperty]
	private bool isSyncing;

	public List<string> ScheduleTypeOptions { get; } = ["Insulin", "Feeding"];

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadSchedulesAsync();
	}

	[RelayCommand]
	private async Task LoadSchedulesAsync()
	{
		if (PetId is null) return;
		var list = await _db.GetSchedulesAsync(PetId);
		Schedules = new ObservableCollection<Schedule>(list);
	}

	[RelayCommand(CanExecute = nameof(CanSaveSchedule))]
	private async Task SaveScheduleAsync()
	{
		if (PetId is null) return;

		var schedule = new Schedule
		{
			PetId = PetId,
			Label = Label,
			ScheduleType = ScheduleType,
			TimeOfDay = TimeOfDay,
			ReminderLeadTimeMinutes = ReminderLeadTimeMinutes,
			IsEnabled = true
		};

		await _db.SaveScheduleAsync(schedule);

		if (!string.IsNullOrEmpty(PetId))
			_ = SyncInBackgroundAsync(PetId);

		// Reset form
		Label = string.Empty;

		await LoadSchedulesAsync();
	}

	private bool CanSaveSchedule() => !string.IsNullOrWhiteSpace(Label);

	[RelayCommand]
	private async Task ToggleScheduleAsync(Schedule schedule)
	{
		schedule.IsEnabled = !schedule.IsEnabled;
		await _db.SaveScheduleAsync(schedule);

		if (!string.IsNullOrEmpty(PetId))
			_ = SyncInBackgroundAsync(PetId);

		await LoadSchedulesAsync();
	}

	[RelayCommand]
	private async Task DeleteScheduleAsync(Schedule schedule)
	{
		await _db.DeleteScheduleAsync(schedule);
		Schedules.Remove(schedule);

		if (!string.IsNullOrEmpty(PetId))
			_ = SyncInBackgroundAsync(PetId);
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
