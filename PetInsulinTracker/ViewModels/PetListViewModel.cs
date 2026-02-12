using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public partial class PetListItemViewModel : ObservableObject
{
	public Pet Pet { get; }

	[ObservableProperty]
	private string lastInsulinText = "";

	[ObservableProperty]
	private string lastFeedingText = "";

	[ObservableProperty]
	private bool showWeight;

	[ObservableProperty]
	private bool showLastInsulin;

	[ObservableProperty]
	private bool showLastFeeding;

	public PetListItemViewModel(Pet pet, string lastInsulinText, string lastFeedingText, bool showWeight, bool showLastInsulin, bool showLastFeeding)
	{
		Pet = pet;
		LastInsulinText = lastInsulinText;
		LastFeedingText = lastFeedingText;
		ShowWeight = showWeight;
		ShowLastInsulin = showLastInsulin;
		ShowLastFeeding = showLastFeeding;
	}
}

public partial class PetListViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private DateTime _lastSyncTime = DateTime.MinValue;
	private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(30);

	public PetListViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
		WeakReferenceMessenger.Default.Register<WeightUnitChangedMessage>(this, (_, _) => _ = LoadPetsAsync());
		WeakReferenceMessenger.Default.Register<PetSavedMessage>(this, (_, _) => _ = LoadPetsAsync());
	}

	[ObservableProperty]
	private ObservableCollection<PetListItemViewModel> pets = [];

	[ObservableProperty]
	private bool isRefreshing;

	[RelayCommand]
	private async Task LoadPetsAsync()
	{
		try
		{
			// Only sync if it's been longer than the sync interval
			if (DateTime.UtcNow - _lastSyncTime >= SyncInterval)
			{
				try
				{
					await _syncService.SyncAllAsync();
					_lastSyncTime = DateTime.UtcNow;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Sync during refresh failed: {ex.Message}");
				}
			}

			var petList = await _db.GetPetsAsync();
			var petViewModels = new List<PetListItemViewModel>();

			foreach (var pet in petList)
			{
				var lastInsulin = await _db.GetLatestInsulinLogAsync(pet.Id);
				var lastInsulinText = lastInsulin is not null
					? $"{lastInsulin.AdministeredAt:g}"
					: "No insulin logged";

				var feedingLogs = await _db.GetFeedingLogsAsync(pet.Id);
				var lastFeeding = feedingLogs.FirstOrDefault();
				var lastFeedingText = lastFeeding is not null
					? $"{lastFeeding.FedAt:g}"
					: "No feeding logged";

				// Get schedules to determine visibility
				var schedules = await _db.GetSchedulesAsync(pet.Id);
				var hasInsulinSchedule = schedules.Any(s => s.ScheduleType == Helpers.Constants.ScheduleTypeInsulin || s.ScheduleType == Helpers.Constants.ScheduleTypeCombined);
				var hasFeedingSchedule = schedules.Any(s => s.ScheduleType == Helpers.Constants.ScheduleTypeFeeding || s.ScheduleType == Helpers.Constants.ScheduleTypeCombined);

				// Determine visibility
				var showWeight = pet.CurrentWeight.HasValue;
				var showLastInsulin = !string.IsNullOrEmpty(pet.InsulinType) || pet.CurrentDoseIU.HasValue || hasInsulinSchedule;
				var showLastFeeding = !string.IsNullOrEmpty(pet.DefaultFoodName) || pet.DefaultFoodAmount.HasValue || hasFeedingSchedule;

				petViewModels.Add(new PetListItemViewModel(pet, lastInsulinText, lastFeedingText, showWeight, showLastInsulin, showLastFeeding));
			}

			Pets = new ObservableCollection<PetListItemViewModel>(petViewModels);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	private async Task GoToAddPetAsync()
	{
		var action = await Shell.Current.DisplayActionSheetAsync(
			"Add a Pet", "Cancel", null, "Add New Pet", "Import Shared Pet");

		switch (action)
		{
			case "Add New Pet":
				await Shell.Current.GoToAsync(nameof(Views.AddEditPetPage));
				break;
			case "Import Shared Pet":
				await Shell.Current.GoToAsync(nameof(Views.ImportPetPage));
				break;
		}
	}

	[RelayCommand]
	private async Task GoToPetDetailAsync(PetListItemViewModel petItem)
	{
		await Shell.Current.GoToAsync($"{nameof(Views.PetDetailPage)}?petId={petItem.Pet.Id}");
	}

	[RelayCommand]
	private static async Task GoToSettingsAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.SettingsPage));
	}
}
