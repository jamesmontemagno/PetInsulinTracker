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

	public PetListItemViewModel(Pet pet, string lastInsulinText, string lastFeedingText)
	{
		Pet = pet;
		LastInsulinText = lastInsulinText;
		LastFeedingText = lastFeedingText;
	}
}

public partial class PetListViewModel : ObservableObject
{
	private readonly IDatabaseService _db;

	public PetListViewModel(IDatabaseService db)
	{
		_db = db;
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
			var petList = await _db.GetPetsAsync();
			var petViewModels = new List<PetListItemViewModel>();

			foreach (var pet in petList)
			{
				var lastInsulin = await _db.GetLatestInsulinLogAsync(pet.Id);
				var lastInsulinText = lastInsulin is not null
					? $"{lastInsulin.DoseIU} IU — {lastInsulin.AdministeredAt:g}"
					: "No insulin logged";

				var feedingLogs = await _db.GetFeedingLogsAsync(pet.Id);
				var lastFeeding = feedingLogs.FirstOrDefault();
				var lastFeedingText = lastFeeding is not null
					? $"{lastFeeding.FoodName} — {lastFeeding.FedAt:g}"
					: "No feeding logged";

				petViewModels.Add(new PetListItemViewModel(pet, lastInsulinText, lastFeedingText));
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
