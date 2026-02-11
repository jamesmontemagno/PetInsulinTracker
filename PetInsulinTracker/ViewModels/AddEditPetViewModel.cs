using System.Diagnostics;
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
	private bool _photoChangedDuringEdit;

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
	private bool takesInsulin;

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
	private bool takesMedication;

	[ObservableProperty]
	private string? petMedication;

	[ObservableProperty]
	private string? photoPath;

	[ObservableProperty]
	private string? photoPreviewSource;

	[ObservableProperty]
	private bool isEditing;

	[ObservableProperty]
	private bool isSaving;

	[ObservableProperty]
	private string? savingStatus;

	public string PageTitle => IsEditing ? "Edit Pet" : "Add Pet";

	public List<string> SpeciesOptions { get; } = ["Dog", "Cat"];
	public List<string> InsulinTypeOptions { get; } = ["Vetsulin", "ProZinc", "NPH (Humulin N)", "Glargine (Lantus)", "Other"];
	public List<string> ConcentrationOptions { get; } = ["U-40", "U-100"];
	public List<string> WeightUnitOptions { get; } = ["lbs", "kg"];
	public List<string> FoodUnitOptions { get; } = ["cups", "grams", "oz", "cans"];
	public List<string> FoodTypeOptions { get; } = ["Dry", "Wet", "Treat"];

	partial void OnTakesInsulinChanged(bool value)
	{
		if (!value)
		{
			InsulinType = null;
			InsulinConcentration = "U-40";
			CurrentDoseIU = null;
		}
	}

	partial void OnTakesMedicationChanged(bool value)
	{
		if (!value)
		{
			PetMedication = null;
		}
	}

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
			PhotoPreviewSource = ResolvePhotoPath(value);
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

	/// <summary>
	/// Resolves a stored photo path (relative or absolute) to the full absolute path.
	/// </summary>
	private static string ResolvePhotoPath(string photoPath)
	{
		if (Path.IsPathRooted(photoPath))
			return photoPath;
		return Path.Combine(FileSystem.AppDataDirectory, photoPath);
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
		TakesInsulin = !string.IsNullOrEmpty(_existingPet.InsulinType) || _existingPet.CurrentDoseIU is not null;
		InsulinType = _existingPet.InsulinType;
		InsulinConcentration = _existingPet.InsulinConcentration ?? "U-40";
		CurrentDoseIU = _existingPet.CurrentDoseIU;
		WeightUnit = _existingPet.WeightUnit;
		CurrentWeight = _existingPet.CurrentWeight;
		DefaultFoodName = _existingPet.DefaultFoodName;
		DefaultFoodAmount = _existingPet.DefaultFoodAmount;
		DefaultFoodUnit = _existingPet.DefaultFoodUnit;
		DefaultFoodType = _existingPet.DefaultFoodType;
		TakesMedication = !string.IsNullOrEmpty(_existingPet.PetMedication);
		PetMedication = _existingPet.PetMedication;
		PhotoPath = _existingPet.PhotoPath;
		PhotoPreviewSource = _existingPet.PhotoSource;
		OnPropertyChanged(nameof(PageTitle));
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveAsync()
	{
		IsSaving = true;
		SavingStatus = "Saving pet info…";

		try
		{
			var pet = _existingPet ?? new Pet();
			var isNew = _existingPet is null;
			var photoChanged = isNew ? !string.IsNullOrEmpty(PhotoPath) : _photoChangedDuringEdit;
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
			pet.PetMedication = PetMedication;
			pet.PhotoPath = PhotoPath;

			await _db.SavePetAsync(pet);

			// Create the pet in the backend
			if (isNew)
			{
				SavingStatus = "Syncing with server…";
				try
				{
					await _syncService.CreatePetAsync(pet);
					pet.IsSynced = true;
					await _db.SavePetAsync(pet);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"CreatePet sync failed (will retry): {ex}");
				}
			}
			else
			{
				SavingStatus = "Syncing…";
				try
				{
					await _syncService.SyncAsync(pet.Id);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Sync failed (will retry): {ex}");
				}
			}

			string? photoUploadError = null;
			if (photoChanged && !string.IsNullOrEmpty(PhotoPath) && pet.AccessLevel != "guest" && !Constants.IsOfflineMode)
			{
				// Ask user if they want to upload the photo
				var uploadPhoto = await Shell.Current.DisplayAlertAsync(
					"Upload Photo",
					"The photo has been saved locally. Would you like to also upload it to the cloud for backup and sharing across devices?",
					"Upload",
					"Keep Local Only");

				if (uploadPhoto)
				{
					SavingStatus = "Uploading photo…";
					try
					{
						var url = await _syncService.UploadPetPhotoThumbnailAsync(pet.Id, PhotoPath);
						if (!string.IsNullOrEmpty(url))
						{
							pet.PhotoUrl = url;
							await _db.SaveSyncedAsync(pet);
						}
						else
						{
							photoUploadError = "Photo could not be uploaded. The image may be in an unsupported format.";
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Photo upload failed: {ex}");
						photoUploadError = $"Photo upload failed: {ex.Message}";
					}
				}
			}

			WeakReferenceMessenger.Default.Send(new PetSavedMessage(pet));

			if (isNew)
			{
				// Guide the user through schedule and vet setup for the new pet
				await PromptPostSaveSetupAsync(pet.Id);
			}
			else
			{
				await Shell.Current.GoToAsync("..");
			}

			if (!string.IsNullOrEmpty(photoUploadError))
			{
				await Shell.Current.DisplayAlertAsync("Photo Upload", photoUploadError, "OK");
			}
		}
		finally
		{
			IsSaving = false;
			SavingStatus = null;
		}
	}

	private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

	/// <summary>
	/// After creating a new pet, guides the user through setting up schedules and vet info,
	/// mirroring the onboarding flow.
	/// </summary>
	private static async Task PromptPostSaveSetupAsync(string petId)
	{
		var action = await Shell.Current.DisplayActionSheetAsync(
			"Pet saved! What would you like to set up next?",
			"I'm Done",
			null,
			"Set Up Schedules",
			"Add Vet Info");

		switch (action)
		{
			case "Set Up Schedules":
				await Shell.Current.GoToAsync($"../{nameof(Views.SchedulePage)}?petId={petId}");
				break;
			case "Add Vet Info":
				await Shell.Current.GoToAsync($"../{nameof(Views.VetInfoPage)}?petId={petId}");
				break;
			default:
				await Shell.Current.GoToAsync("..");
				break;
		}
	}

	/// <summary>
	/// Copies a picked photo to the app data directory so it persists.
	/// Stores and returns the full absolute path.
	/// SkiaSharp conversion/orientation is only done at upload time via SyncService.
	/// </summary>
	private async Task<string?> CopyPhotoToAppDataAsync(FileResult result)
	{
		var destDir = Path.Combine(FileSystem.AppDataDirectory, "pet_photos");
		Directory.CreateDirectory(destDir);

		var ext = Path.GetExtension(result.FileName)?.ToLowerInvariant();
		if (string.IsNullOrEmpty(ext)) ext = ".jpg";
		var fileName = $"{(_existingPet?.Id ?? Guid.NewGuid().ToString())}{ext}";
		var destPath = Path.Combine(destDir, fileName);

		using var sourceStream = await result.OpenReadAsync();
		using var destStream = File.Create(destPath);
		await sourceStream.CopyToAsync(destStream);

		return destPath;
	}

	[RelayCommand]
	private async Task PickPhotoAsync()
	{
		try
		{
			var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
			{
				Title = "Select pet photo",
				SelectionLimit = 1
			});

			var result = results?.FirstOrDefault();
			if (result is null) return;

			PhotoPath = await CopyPhotoToAppDataAsync(result);
			_photoChangedDuringEdit = true;
		}
		catch (PermissionException)
		{
			await Shell.Current.DisplayAlertAsync("Permission Required", "Photo library permission is required. Please enable it in Settings.", "OK");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"PickPhoto failed: {ex}");
		}
	}

	[RelayCommand]
	private async Task TakePhotoAsync()
	{
		try
		{
			if (!MediaPicker.Default.IsCaptureSupported)
			{
				await Shell.Current.DisplayAlertAsync("Camera", "Camera is not available on this device.", "OK");
				return;
			}

			var result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
			{
				Title = "Take pet photo"
			});

			if (result is null) return;

			PhotoPath = await CopyPhotoToAppDataAsync(result);
			_photoChangedDuringEdit = true;
		}
		catch (PermissionException)
		{
			await Shell.Current.DisplayAlertAsync("Permission Required", "Camera permission is required to take photos. Please enable it in Settings.", "OK");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"TakePhoto failed: {ex}");
		}
	}
}
