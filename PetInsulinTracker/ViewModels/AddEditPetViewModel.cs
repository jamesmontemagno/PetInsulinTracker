using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public sealed class PetSavedMessage(Pet pet) : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<Pet>(pet);

[QueryProperty(nameof(PetId), "petId")]
public partial class AddEditPetViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private Pet? _existingPet;

	public AddEditPetViewModel(IDatabaseService db)
	{
		_db = db;
	}

	[ObservableProperty]
	private string? petId;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
	private string name = string.Empty;

	[ObservableProperty]
	private string species = "Dog";

	[ObservableProperty]
	private string breed = string.Empty;

	[ObservableProperty]
	private DateTime? dateOfBirth;

	[ObservableProperty]
	private string? insulinType;

	[ObservableProperty]
	private string insulinConcentration = "U-40";

	[ObservableProperty]
	private double? currentDoseIU;

	[ObservableProperty]
	private string weightUnit = "lbs";

	[ObservableProperty]
	private double? currentWeight;

	[ObservableProperty]
	private bool isEditing;

	public string PageTitle => IsEditing ? "Edit Pet" : "Add Pet";

	public List<string> SpeciesOptions { get; } = ["Dog", "Cat"];
	public List<string> InsulinTypeOptions { get; } = ["Vetsulin", "ProZinc", "NPH (Humulin N)", "Glargine (Lantus)", "Other"];
	public List<string> ConcentrationOptions { get; } = ["U-40", "U-100"];
	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			_ = LoadPetAsync(value);
		}
	}

	private async Task LoadPetAsync(string id)
	{
		_existingPet = await _db.GetPetAsync(id);
		if (_existingPet is null) return;

		IsEditing = true;
		Name = _existingPet.Name;
		Species = _existingPet.Species;
		Breed = _existingPet.Breed;
		DateOfBirth = _existingPet.DateOfBirth;
		InsulinType = _existingPet.InsulinType;
		InsulinConcentration = _existingPet.InsulinConcentration ?? "U-40";
		CurrentDoseIU = _existingPet.CurrentDoseIU;
		WeightUnit = _existingPet.WeightUnit;
		CurrentWeight = _existingPet.CurrentWeight;
		OnPropertyChanged(nameof(PageTitle));
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		var pet = _existingPet ?? new Pet();
		pet.Name = Name;
		pet.Species = Species;
		pet.Breed = Breed;
		pet.DateOfBirth = DateOfBirth;
		pet.InsulinType = InsulinType;
		pet.InsulinConcentration = InsulinConcentration;
		pet.CurrentDoseIU = CurrentDoseIU;
		pet.WeightUnit = WeightUnit;
		pet.CurrentWeight = CurrentWeight;

		await _db.SavePetAsync(pet);
		WeakReferenceMessenger.Default.Send(new PetSavedMessage(pet));
		await Shell.Current.GoToAsync("..");
	}

	private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
}
