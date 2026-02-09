using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;

namespace PetInsulinTracker.ViewModels;

public sealed class PetSavedMessage(Pet pet) : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<Pet>(pet);
public sealed class WeightUnitChangedMessage(string unit) : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<string>(unit);

[QueryProperty(nameof(PetId), "petId")]
public partial class AddEditPetViewModel : ObservableObject
{
	private readonly IDatabaseService _db;
	private readonly ISyncService _syncService;
	private Pet? _existingPet;

	public AddEditPetViewModel(IDatabaseService db, ISyncService syncService)
	{
		_db = db;
		_syncService = syncService;
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
	private string weightUnit = Preferences.Get("default_weight_unit", "lbs");

	[ObservableProperty]
	private double? currentWeight;

	[ObservableProperty]
	private string? defaultFoodName;

	[ObservableProperty]
	private double? defaultFoodAmount;

	[ObservableProperty]
	private string defaultFoodUnit = "cups";

	[ObservableProperty]
	private string defaultFoodType = "Dry";

	[ObservableProperty]
	private string? photoPath;

	[ObservableProperty]
	private string? photoPreviewSource;

	[ObservableProperty]
	private bool isEditing;

	public string PageTitle => IsEditing ? "Edit Pet" : "Add Pet";

	public List<string> SpeciesOptions { get; } = ["Dog", "Cat"];
	public List<string> InsulinTypeOptions { get; } = ["Vetsulin", "ProZinc", "NPH (Humulin N)", "Glargine (Lantus)", "Other"];
	public List<string> ConcentrationOptions { get; } = ["U-40", "U-100"];
	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];
	public List<string> FoodUnitOptions { get; } = ["cups", "grams", "oz", "cans"];
	public List<string> FoodTypeOptions { get; } = ["Dry", "Wet", "Treat"];

	partial void OnPetIdChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			_ = LoadPetAsync(value);
		}
	}

	partial void OnPhotoPathChanged(string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			PhotoPreviewSource = value;
		}
		else if (_existingPet is not null)
		{
			PhotoPreviewSource = _existingPet.PhotoUrl;
		}
		else
		{
			PhotoPreviewSource = null;
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
		DefaultFoodName = _existingPet.DefaultFoodName;
		DefaultFoodAmount = _existingPet.DefaultFoodAmount;
		DefaultFoodUnit = _existingPet.DefaultFoodUnit;
		DefaultFoodType = _existingPet.DefaultFoodType;
		PhotoPath = _existingPet.PhotoPath;
		PhotoPreviewSource = !string.IsNullOrEmpty(_existingPet.PhotoPath)
			? _existingPet.PhotoPath
			: _existingPet.PhotoUrl;
		OnPropertyChanged(nameof(PageTitle));
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		var pet = _existingPet ?? new Pet();
		var isNew = _existingPet is null;
		var photoChanged = _existingPet?.PhotoPath != PhotoPath;
		if (isNew)
		{
			pet.OwnerId = Constants.DeviceUserId;
			pet.OwnerName = Constants.OwnerName;
		}
		pet.Name = Name;
		pet.Species = Species;
		pet.Breed = Breed;
		pet.DateOfBirth = DateOfBirth;
		pet.InsulinType = InsulinType;
		pet.InsulinConcentration = InsulinConcentration;
		pet.CurrentDoseIU = CurrentDoseIU;
		pet.WeightUnit = WeightUnit;
		pet.CurrentWeight = CurrentWeight;
		pet.DefaultFoodName = DefaultFoodName;
		pet.DefaultFoodAmount = DefaultFoodAmount;
		pet.DefaultFoodUnit = DefaultFoodUnit;
		pet.DefaultFoodType = DefaultFoodType;
		pet.PhotoPath = PhotoPath;

		await _db.SavePetAsync(pet);

		// Create the pet in the backend
		if (isNew)
		{
			try
			{
				await _syncService.CreatePetAsync(pet);
				pet.IsSynced = true;
				await _db.SavePetAsync(pet);
			}
			catch
			{
				// Will retry on next sync
			}
		}
		else
		{
			_ = _syncService.SyncAsync(pet.Id);
		}

		if (photoChanged && !string.IsNullOrEmpty(PhotoPath) && pet.AccessLevel != "guest")
		{
			try
			{
				var url = await _syncService.UploadPetPhotoThumbnailAsync(pet.Id, PhotoPath);
				if (!string.IsNullOrEmpty(url))
				{
					pet.PhotoUrl = url;
					await _db.SaveSyncedAsync(pet);
				}
			}
			catch
			{
				// Upload can retry later
			}
		}

		WeakReferenceMessenger.Default.Send(new PetSavedMessage(pet));
		await Shell.Current.GoToAsync("..");
	}

	private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

	[RelayCommand]
	private async Task PickPhotoAsync()
	{
		try
		{
			var results = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
			{
				Title = "Select pet photo"
			});

			var result = results?.FirstOrDefault();
			if (result is null) return;

			// Copy to app data directory so it persists
			var destDir = Path.Combine(FileSystem.AppDataDirectory, "pet_photos");
			Directory.CreateDirectory(destDir);
			var destPath = Path.Combine(destDir, $"{(_existingPet?.Id ?? Guid.NewGuid().ToString())}{Path.GetExtension(result.FileName)}");

			using var sourceStream = await result.OpenReadAsync();
			using var destStream = File.Create(destPath);
			await sourceStream.CopyToAsync(destStream);

			PhotoPath = destPath;
		}
		catch (Exception)
		{
			// User cancelled or permission denied
		}
	}

	[RelayCommand]
	private async Task TakePhotoAsync()
	{
		try
		{
			var result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
			{
				Title = "Take pet photo"
			});

			if (result is null) return;

			var destDir = Path.Combine(FileSystem.AppDataDirectory, "pet_photos");
			Directory.CreateDirectory(destDir);
			var destPath = Path.Combine(destDir, $"{(_existingPet?.Id ?? Guid.NewGuid().ToString())}{Path.GetExtension(result.FileName)}");

			using var sourceStream = await result.OpenReadAsync();
			using var destStream = File.Create(destPath);
			await sourceStream.CopyToAsync(destStream);

			PhotoPath = destPath;
		}
		catch (Exception)
		{
			// User cancelled or permission denied
		}
	}
}
