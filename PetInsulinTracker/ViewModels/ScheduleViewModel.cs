using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class ScheduleViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private readonly INotificationService _notifications;
	private string? _editingScheduleId;

	public ScheduleViewModel(IDatabaseService db, ISyncService syncService, INotificationService notifications)
	{
		_db = db;
		_syncService = syncService;
		_notifications = notifications;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<Schedule> schedules = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveScheduleCommand))]
	private bool canEdit;

	// New schedule fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveScheduleCommand))]
	private string label = string.Empty;

	[ObservableProperty]
	private string scheduleType = Constants.ScheduleTypeInsulin;

	[ObservableProperty]
	private TimeSpan timeOfDay = new(8, 0, 0);

	[ObservableProperty]
	private int reminderLeadTimeMinutes = 15;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SaveButtonText))]
	private bool isEditing;

	[ObservableProperty]
	private bool isSyncing;

	public List<string> ScheduleTypeOptions { get; } = [Constants.ScheduleTypeInsulin, Constants.ScheduleTypeFeeding, Constants.ScheduleTypeCombined];

	public string SaveButtonText => IsEditing ? "Save Changes" : "Add Schedule";

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
			_ = LoadSchedulesAsync();
	}

	[RelayCommand]
	private async Task LoadSchedulesAsync()
	{
		if (PetId is null) return;
		var pet = await _db.GetPetAsync(PetId);
		CanEdit = pet is not null && pet.AccessLevel != "guest";
		var list = await _db.GetSchedulesAsync(PetId);
		Schedules = new ObservableCollection<Schedule>(list);
	}

	[RelayCommand(CanExecute = nameof(CanSaveSchedule))]
	private async Task SaveScheduleAsync()
	{
		if (PetId is null || !CanEdit) return;

		Schedule schedule;
		if (!string.IsNullOrEmpty(_editingScheduleId))
		{
			schedule = await _db.GetScheduleAsync(_editingScheduleId) ?? new Schedule { Id = _editingScheduleId };
			schedule.PetId = PetId;
		}
		else
		{
			schedule = new Schedule { PetId = PetId };
		}

		schedule.Label = Label;
		schedule.ScheduleType = ScheduleType;
		schedule.TimeOfDay = TimeOfDay;
		schedule.ReminderLeadTimeMinutes = ReminderLeadTimeMinutes;
		schedule.IsEnabled = true;

		await _db.SaveScheduleAsync(schedule);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		await UpdateNotificationsAsync(PetId);

		ResetForm();

		await LoadSchedulesAsync();
	}

	private bool CanSaveSchedule() => CanEdit && !string.IsNullOrWhiteSpace(Label);

	[RelayCommand]
	private async Task DeleteScheduleAsync(Schedule schedule)
	{
		if (!CanEdit) return;
		await _db.DeleteScheduleAsync(schedule);
		Schedules.Remove(schedule);

		if (!string.IsNullOrEmpty(PetId))
			await SyncInBackgroundAsync(PetId);

		if (!string.IsNullOrEmpty(PetId))
			await UpdateNotificationsAsync(PetId);
	}

	[RelayCommand]
	private void EditSchedule(Schedule schedule)
	{
		if (!CanEdit) return;
		_editingScheduleId = schedule.Id;
		Label = schedule.Label;
		ScheduleType = schedule.ScheduleType;
		TimeOfDay = schedule.TimeOfDay;
		ReminderLeadTimeMinutes = schedule.ReminderLeadTimeMinutes;
		IsEditing = true;
	}

	[RelayCommand]
	private void CancelEdit()
	{
		ResetForm();
	}

	private void ResetForm()
	{
		_editingScheduleId = null;
		Label = string.Empty;
		ScheduleType = Constants.ScheduleTypeInsulin;
		TimeOfDay = new TimeSpan(8, 0, 0);
		ReminderLeadTimeMinutes = 15;
		IsEditing = false;
	}

	private async Task UpdateNotificationsAsync(string petId)
	{
		if (!Preferences.Get(Constants.NotificationsEnabledKey, true))
			return;

		await _notifications.ScheduleNotificationsForPetAsync(petId);
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
