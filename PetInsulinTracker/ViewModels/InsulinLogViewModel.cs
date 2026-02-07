using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class InsulinLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public InsulinLogViewModel(IDatabaseService db)
	{
		_db = db;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<InsulinLog> logs = [];

	// New log entry fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
	private double doseIU;

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private TimeSpan logTime = DateTime.Now.TimeOfDay;

	[ObservableProperty]
	private string? injectionSite;

	[ObservableProperty]
	private string? notes;

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
			logList = logList.Where(l => l.LoggedBy == Constants.OwnerName).ToList();

		Logs = new ObservableCollection<InsulinLog>(logList);
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
			LoggedBy = Constants.OwnerName
		};

		await _db.SaveInsulinLogAsync(log);

		// Reset form
		InjectionSite = null;
		Notes = null;
		LogDate = DateTime.Today;
		LogTime = DateTime.Now.TimeOfDay;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => DoseIU > 0;

	[RelayCommand]
	private async Task DeleteLogAsync(InsulinLog log)
	{
		await _db.DeleteInsulinLogAsync(log);
		Logs.Remove(log);
	}
}
