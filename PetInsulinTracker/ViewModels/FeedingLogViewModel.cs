using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

[QueryProperty(nameof(PetId), "petId")]
public partial class FeedingLogViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public FeedingLogViewModel(IDatabaseService db)
	{
		_db = db;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	private ObservableCollection<FeedingLog> logs = [];

	// New log entry fields
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveLogCommand))]
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

		Logs = new ObservableCollection<FeedingLog>(logList);
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

		// Reset form to pet defaults
		Notes = null;
		LogDate = DateTime.Today;
		LogTime = DateTime.Now.TimeOfDay;

		await LoadLogsAsync();
	}

	private bool CanSaveLog() => !string.IsNullOrWhiteSpace(FoodName);

	[RelayCommand]
	private async Task DeleteLogAsync(FeedingLog log)
	{
		await _db.DeleteFeedingLogAsync(log);
		Logs.Remove(log);
	}
}
