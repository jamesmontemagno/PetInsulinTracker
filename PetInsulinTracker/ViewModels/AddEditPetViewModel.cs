using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Services;
using SkiaSharp;

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
			if (photoChanged && !string.IsNullOrEmpty(PhotoPath) && pet.AccessLevel != "guest")
			{
				// Ask user if they want to upload the photo
				var uploadPhoto = await Shell.Current.DisplayAlertAsync(
					"Upload Photo",
					"Would you like to upload this photo to the cloud? You can always use the local photo if you choose not to upload.",
					"Upload",
					"Use Local Only");

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
	/// Copies a photo to the app data directory, converting HEIC/HEIF to JPEG
	/// since SkiaSharp may not decode HEIC on all platforms.
	/// Also applies EXIF orientation correction so images display upright.
	/// </summary>
	private async Task<string?> CopyAndConvertPhotoAsync(FileResult result)
	{
		var destDir = Path.Combine(FileSystem.AppDataDirectory, "pet_photos");
		Directory.CreateDirectory(destDir);

		var ext = Path.GetExtension(result.FileName)?.ToLowerInvariant();
		var isHeic = ext is ".heic" or ".heif";
		// Always output JPEG so EXIF orientation is baked in
		var destPath = Path.Combine(destDir, $"{(_existingPet?.Id ?? Guid.NewGuid().ToString())}.jpg");

		using var sourceStream = await result.OpenReadAsync();
		using var memStream = new MemoryStream();
		await sourceStream.CopyToAsync(memStream);
		memStream.Position = 0;

		using var bitmap = SKBitmap.Decode(memStream);
		if (bitmap is null)
		{
			// Fallback: copy raw bytes
			Debug.WriteLine($"SKBitmap.Decode failed for {result.FileName}, copying raw file");
			memStream.Position = 0;
			var rawDestPath = Path.Combine(destDir, $"{(_existingPet?.Id ?? Guid.NewGuid().ToString())}{ext}");
			using var fallbackDest = File.Create(rawDestPath);
			await memStream.CopyToAsync(fallbackDest);
			return rawDestPath;
		}

		// Read EXIF orientation and apply correction
		memStream.Position = 0;
		var corrected = ApplyExifOrientation(bitmap, memStream);
		var final = corrected ?? bitmap;

		using var image = SKImage.FromBitmap(final);
		using var jpegData = image.Encode(SKEncodedImageFormat.Jpeg, 95);
		using var destStream = File.Create(destPath);
		jpegData.SaveTo(destStream);

		corrected?.Dispose();

		return destPath;
	}

	/// <summary>
	/// Reads EXIF orientation from a stream and returns a corrected bitmap,
	/// or null if no correction is needed.
	/// </summary>
	private static SKBitmap? ApplyExifOrientation(SKBitmap bitmap, Stream stream)
	{
		try
		{
			using var codec = SKCodec.Create(stream);
			if (codec is null) return null;

			var origin = codec.EncodedOrigin;
			if (origin == SKEncodedOrigin.Default || origin == SKEncodedOrigin.TopLeft)
				return null;

			return ApplyOrientation(bitmap, origin);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Creates a new bitmap with the EXIF orientation transform applied.
	/// </summary>
	private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
	{
		var needsSwap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
			or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

		var width = needsSwap ? bitmap.Height : bitmap.Width;
		var height = needsSwap ? bitmap.Width : bitmap.Height;

		var result = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType);
		using var canvas = new SKCanvas(result);

		switch (origin)
		{
			case SKEncodedOrigin.TopRight:
				canvas.Scale(-1, 1, width / 2f, 0);
				break;
			case SKEncodedOrigin.BottomRight:
				canvas.RotateDegrees(180, width / 2f, height / 2f);
				break;
			case SKEncodedOrigin.BottomLeft:
				canvas.Scale(1, -1, 0, height / 2f);
				break;
			case SKEncodedOrigin.LeftTop:
				canvas.Translate(0, 0);
				canvas.RotateDegrees(270, 0, 0);
				canvas.Translate(-height, 0);
				canvas.Scale(-1, 1, height / 2f, 0);
				break;
			case SKEncodedOrigin.RightTop:
				canvas.Translate(width, 0);
				canvas.RotateDegrees(90);
				break;
			case SKEncodedOrigin.RightBottom:
				canvas.Translate(width, 0);
				canvas.RotateDegrees(90);
				canvas.Scale(-1, 1, bitmap.Height / 2f, 0);
				break;
			case SKEncodedOrigin.LeftBottom:
				canvas.Translate(0, height);
				canvas.RotateDegrees(270);
				break;
		}

		canvas.DrawBitmap(bitmap, 0, 0);
		return result;
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

			PhotoPath = await CopyAndConvertPhotoAsync(result);
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

			PhotoPath = await CopyAndConvertPhotoAsync(result);
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
