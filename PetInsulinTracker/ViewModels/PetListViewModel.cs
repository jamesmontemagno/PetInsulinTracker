using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

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
	private ObservableCollection<Pet> pets = [];

	[ObservableProperty]
	private bool isRefreshing;

	[RelayCommand]
	private async Task LoadPetsAsync()
	{
		try
		{
			var petList = await _db.GetPetsAsync();
			Pets = new ObservableCollection<Pet>(petList);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	private async Task GoToAddPetAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.AddEditPetPage));
	}

	[RelayCommand]
	private async Task GoToPetDetailAsync(Pet pet)
	{
		await Shell.Current.GoToAsync($"{nameof(Views.PetDetailPage)}?petId={pet.Id}");
	}

	[RelayCommand]
	private static async Task GoToSettingsAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.SettingsPage));
	}
}
