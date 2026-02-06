using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class WeightLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public WeightLogViewModel(IDatabaseService db)
	{
		_db = db;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<WeightLog> logs = [];

	// New log entry fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
	private double weight;

	[ObservableProperty]
	private string unit = "lbs";

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private string? notes;

	[ObservableProperty]
	private string trendText = "";

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
		if (pet is not null)
			Unit = pet.WeightUnit;

		var logList = await _db.GetWeightLogsAsync(PetId);
		Logs = new ObservableCollection<WeightLog>(logList);
		UpdateTrend();
	}

	private void UpdateTrend()
	{
		if (Logs.Count < 2)
		{
			TrendText = "";
			return;
		}

		var latest = Logs[0].Weight;
		var previous = Logs[1].Weight;
		var diff = latest - previous;
		TrendText = diff switch
		{
			> 0 => $"↑ +{diff:F1} {Unit} since last",
			< 0 => $"↓ {diff:F1} {Unit} since last",
			_ => "→ No change"
		};
	}

	[RelayCommand(CanExecute = nameof(CanSaveLog))]
	private async Task SaveLogAsync()
	{
		if (PetId is null) return;

		var log = new WeightLog
		{
			PetId = PetId,
			Weight = Weight,
			Unit = Unit,
			RecordedAt = LogDate,
			Notes = Notes
		};

		await _db.SaveWeightLogAsync(log);

		// Also update pet's current weight
		var pet = await _db.GetPetAsync(PetId);
		if (pet is not null)
		{
			pet.CurrentWeight = Weight;
			await _db.SavePetAsync(pet);
		}

		// Reset form
		Weight = 0;
		Notes = null;
		LogDate = DateTime.Today;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => Weight > 0;

	[RelayCommand]
	private async Task DeleteLogAsync(WeightLog log)
	{
		await _db.DeleteWeightLogAsync(log);
		Logs.Remove(log);
		UpdateTrend();
	}
}
