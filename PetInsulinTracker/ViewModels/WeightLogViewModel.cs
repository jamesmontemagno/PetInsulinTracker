using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class WeightLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;

	public WeightLogViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
		WeakReferenceMessenger.Default.Register<WeightUnitChangedMessage>(this, (r, m) =>
		{
			((WeightLogViewModel)r).Unit = m.Value;
		});
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
	private string unit = Preferences.Get("default_weight_unit", "lbs");

	[ObservableProperty]
	private DateTime logDate = DateTime.Today;

	[ObservableProperty]
	private string? notes;

	[ObservableProperty]
	private string trendText = "";

	[ObservableProperty]
	private List<double> chartData = [];

	[ObservableProperty]
	private List<string> chartLabels = [];

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

		// Filter for guest access — only show own logs
		if (pet?.AccessLevel == "guest")
			logList = logList.Where(l => l.LoggedById == Constants.DeviceUserId).ToList();

		Logs = new ObservableCollection<WeightLog>(logList);
		UpdateTrend();
	}

	private void UpdateTrend()
	{
		if (Logs.Count < 2)
		{
			TrendText = "";
			ChartData = [];
			ChartLabels = [];
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

		// Build chart data (oldest to newest, max 20 points)
		var chartLogs = Logs.Reverse().TakeLast(20).ToList();
		ChartData = chartLogs.Select(l => l.Weight).ToList();
		ChartLabels = chartLogs.Select(l => l.RecordedAt.ToString("M/d")).ToList();
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
			Notes = Notes,
			LoggedBy = Constants.OwnerName,
			LoggedById = Constants.DeviceUserId
		};

		await _db.SaveWeightLogAsync(log);

		// Also update pet's current weight
		var pet = await _db.GetPetAsync(PetId);
		if (pet is not null)
		{
			pet.CurrentWeight = Weight;
			await _db.SavePetAsync(pet);

			if (!string.IsNullOrEmpty(pet.Id))
				_ = _syncService.SyncAsync(pet.Id);
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
