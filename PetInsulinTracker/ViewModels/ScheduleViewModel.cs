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

	public ScheduleViewModel(IDatabaseService db)
	{
		_db = db;
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
	private TimeSpan timeOfDay = new(7, 0, 0);

	[ObservableProperty]
	private int reminderLeadTimeMinutes = 15;

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
		await LoadSchedulesAsync();
	}

	[RelayCommand]
	private async Task DeleteScheduleAsync(Schedule schedule)
	{
		await _db.DeleteScheduleAsync(schedule);
		Schedules.Remove(schedule);
	}
}
