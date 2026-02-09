using System.Net.Http.Json;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Shared;
using PetInsulinTracker.Shared.DTOs;
using SkiaSharp;

namespace PetInsulinTracker.Services;

public class SyncService : ISyncService
{
	private readonly IDatabaseService _db;
	private readonly HttpClient _http;

	public SyncService(IDatabaseService db, HttpClient http)
	{
		_db = db;
		_http = http;
	}

	public async Task CreatePetAsync(Pet pet)
	{
		if (Constants.IsOfflineMode) return;

		var request = new CreatePetRequest
		{
			Id = pet.Id,
			DeviceUserId = Constants.DeviceUserId,
			OwnerName = Constants.OwnerName,
			Name = pet.Name,
			Species = pet.Species,
			Breed = pet.Breed,
			DateOfBirth = pet.DateOfBirth,
			InsulinType = pet.InsulinType,
			InsulinConcentration = pet.InsulinConcentration,
			CurrentDoseIU = pet.CurrentDoseIU,
			WeightUnit = pet.WeightUnit,
			CurrentWeight = pet.CurrentWeight,
			DefaultFoodName = pet.DefaultFoodName,
			DefaultFoodAmount = pet.DefaultFoodAmount,
			DefaultFoodUnit = pet.DefaultFoodUnit,
			DefaultFoodType = pet.DefaultFoodType
		};

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/pets", request, AppJsonSerializerContext.Default.CreatePetRequest);
		response.EnsureSuccessStatusCode();
	}

	public async Task<string> GenerateShareCodeAsync(string petId, string accessLevel = "full")
	{
		if (Constants.IsOfflineMode)
			throw new InvalidOperationException("Share codes are not available in offline mode.");

		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/share/generate",
			new ShareCodeRequest { PetId = petId, AccessLevel = accessLevel, OwnerId = Constants.DeviceUserId },
			AppJsonSerializerContext.Default.ShareCodeRequest);

		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.ShareCodeResponse);
		return result?.ShareCode ?? throw new InvalidOperationException("No share code received");
	}

	public async Task RedeemShareCodeAsync(string shareCode)
	{
		if (Constants.IsOfflineMode)
			throw new InvalidOperationException("Share codes are not available in offline mode.");

		var request = new RedeemShareCodeRequest
		{
			ShareCode = shareCode,
			DeviceUserId = Constants.DeviceUserId,
			DisplayName = Constants.OwnerName
		};

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/share/redeem", request, AppJsonSerializerContext.Default.RedeemShareCodeRequest);
		response.EnsureSuccessStatusCode();

		var data = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.RedeemShareCodeResponse);
		if (data is null) throw new InvalidOperationException("No data received");

		// Import pet
		var pet = new Pet
		{
			Id = data.Pet.Id,
			OwnerId = data.Pet.OwnerId,
			OwnerName = data.Pet.OwnerName,
			AccessLevel = data.Pet.AccessLevel,
			Name = data.Pet.Name,
			Species = data.Pet.Species,
			Breed = data.Pet.Breed,
			DateOfBirth = data.Pet.DateOfBirth,
			PhotoUrl = data.Pet.PhotoUrl,
			InsulinType = data.Pet.InsulinType,
			InsulinConcentration = data.Pet.InsulinConcentration,
			CurrentDoseIU = data.Pet.CurrentDoseIU,
			WeightUnit = data.Pet.WeightUnit,
			CurrentWeight = data.Pet.CurrentWeight,
			DefaultFoodName = data.Pet.DefaultFoodName,
			DefaultFoodAmount = data.Pet.DefaultFoodAmount,
			DefaultFoodUnit = data.Pet.DefaultFoodUnit,
			DefaultFoodType = data.Pet.DefaultFoodType,
			LastModified = data.Pet.LastModified,
			IsSynced = true
		};
		await _db.SavePetAsync(pet);

		foreach (var l in data.InsulinLogs)
		{
			await _db.SaveInsulinLogAsync(new InsulinLog
			{
				Id = l.Id, PetId = l.PetId, DoseIU = l.DoseIU,
				AdministeredAt = l.AdministeredAt, InjectionSite = l.InjectionSite,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified, IsSynced = true
			});
		}

		foreach (var l in data.FeedingLogs)
		{
			await _db.SaveFeedingLogAsync(new FeedingLog
			{
				Id = l.Id, PetId = l.PetId, FoodName = l.FoodName,
				Amount = l.Amount, Unit = l.Unit, FoodType = l.FoodType,
				FedAt = l.FedAt, Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified, IsSynced = true
			});
		}

		foreach (var l in data.WeightLogs)
		{
			await _db.SaveWeightLogAsync(new WeightLog
			{
				Id = l.Id, PetId = l.PetId, Weight = l.Weight,
				Unit = l.Unit, RecordedAt = l.RecordedAt,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified, IsSynced = true
			});
		}

		if (data.VetInfo is not null)
		{
			await _db.SaveVetInfoAsync(new VetInfo
			{
				Id = data.VetInfo.Id, PetId = data.VetInfo.PetId,
				VetName = data.VetInfo.VetName, ClinicName = data.VetInfo.ClinicName,
				Phone = data.VetInfo.Phone, EmergencyPhone = data.VetInfo.EmergencyPhone,
				Address = data.VetInfo.Address, Email = data.VetInfo.Email,
				Notes = data.VetInfo.Notes, LastModified = data.VetInfo.LastModified, IsSynced = true
			});
		}

		foreach (var s in data.Schedules)
		{
			await _db.SaveScheduleAsync(new Schedule
			{
				Id = s.Id, PetId = s.PetId, ScheduleType = s.ScheduleType,
				Label = s.Label, TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified, IsSynced = true
			});
		}
	}

	public async Task<List<SharedUserDto>> GetSharedUsersAsync(string petId)
	{
		if (Constants.IsOfflineMode) return [];

		var response = await _http.GetAsync($"{Constants.ApiBaseUrl}/share/pet/{petId}/users");
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.SharedUsersResponse);
		return result?.Users ?? [];
	}

	public async Task RevokeAccessAsync(string petId, string deviceUserId)
	{
		if (Constants.IsOfflineMode) return;

		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/share/revoke",
			new RevokeAccessRequest { PetId = petId, DeviceUserId = deviceUserId },
			AppJsonSerializerContext.Default.RevokeAccessRequest);
		response.EnsureSuccessStatusCode();
	}

	public async Task LeavePetAsync(string petId)
	{
		if (Constants.IsOfflineMode)
			throw new InvalidOperationException("Leaving a shared pet is not available in offline mode.");

		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/share/leave",
			new LeavePetRequest { PetId = petId, DeviceUserId = Constants.DeviceUserId },
			AppJsonSerializerContext.Default.LeavePetRequest);

		response.EnsureSuccessStatusCode();
	}

	public async Task DeletePetAsync(string petId)
	{
		if (Constants.IsOfflineMode)
			throw new InvalidOperationException("Deleting a pet is not available in offline mode.");

		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/pets/delete",
			new DeletePetRequest { PetId = petId, OwnerId = Constants.DeviceUserId },
			AppJsonSerializerContext.Default.DeletePetRequest);

		response.EnsureSuccessStatusCode();
	}

	public async Task<string?> UploadPetPhotoThumbnailAsync(string petId, string photoPath)
	{
		if (Constants.IsOfflineMode || string.IsNullOrWhiteSpace(photoPath)) return null;

		var bytes = CreateThumbnailJpeg(photoPath, 256, 80);
		if (bytes.Length == 0) return null;

		var request = new PetPhotoUploadRequest
		{
			PetId = petId,
			DeviceUserId = Constants.DeviceUserId,
			Base64Image = Convert.ToBase64String(bytes)
		};

		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/pets/{petId}/photo-thumbnail",
			request,
			AppJsonSerializerContext.Default.PetPhotoUploadRequest);

		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.PetPhotoUploadResponse);
		return result?.PhotoUrl;
	}

	public async Task SyncAsync(string petId)
	{
		if (Constants.IsOfflineMode) return;

		var pet = await _db.GetPetAsync(petId);
		if (pet is null) return;

		var lastSyncStr = Preferences.Get($"lastSync_{petId}", string.Empty);
		var lastSync = string.IsNullOrEmpty(lastSyncStr)
			? DateTimeOffset.MinValue
			: DateTimeOffset.Parse(lastSyncStr);

		// Gather unsynced local data scoped to this pet
		var unsyncedPets = await _db.GetUnsyncedAsync<Pet>(pet.Id);
		var unsyncedInsulin = await _db.GetUnsyncedAsync<InsulinLog>(pet.Id);
		var unsyncedFeeding = await _db.GetUnsyncedAsync<FeedingLog>(pet.Id);
		var unsyncedWeight = await _db.GetUnsyncedAsync<WeightLog>(pet.Id);
		var unsyncedVetInfo = await _db.GetUnsyncedAsync<VetInfo>(pet.Id);
		var unsyncedSchedules = await _db.GetUnsyncedAsync<Schedule>(pet.Id);

		// Always include the pet so the server can auto-create if needed
		var petDtos = unsyncedPets.Select(p => new PetDto
		{
			Id = p.Id, OwnerId = p.OwnerId, OwnerName = p.OwnerName,
			AccessLevel = p.AccessLevel,
			Name = p.Name, Species = p.Species, Breed = p.Breed,
			DateOfBirth = p.DateOfBirth, InsulinType = p.InsulinType,
			PhotoUrl = p.PhotoUrl,
			InsulinConcentration = p.InsulinConcentration, CurrentDoseIU = p.CurrentDoseIU,
			WeightUnit = p.WeightUnit, CurrentWeight = p.CurrentWeight,
			DefaultFoodName = p.DefaultFoodName,
			DefaultFoodAmount = p.DefaultFoodAmount,
			DefaultFoodUnit = p.DefaultFoodUnit,
			DefaultFoodType = p.DefaultFoodType,
			LastModified = p.LastModified,
			IsDeleted = p.IsDeleted
		}).ToList();

		if (!petDtos.Any(p => p.Id == pet.Id))
		{
			petDtos.Add(new PetDto
			{
				Id = pet.Id, OwnerId = pet.OwnerId, OwnerName = pet.OwnerName,
				AccessLevel = pet.AccessLevel,
				Name = pet.Name, Species = pet.Species, Breed = pet.Breed,
				DateOfBirth = pet.DateOfBirth, InsulinType = pet.InsulinType,
				PhotoUrl = pet.PhotoUrl,
				InsulinConcentration = pet.InsulinConcentration, CurrentDoseIU = pet.CurrentDoseIU,
				WeightUnit = pet.WeightUnit, CurrentWeight = pet.CurrentWeight,
				DefaultFoodName = pet.DefaultFoodName,
				DefaultFoodAmount = pet.DefaultFoodAmount,
				DefaultFoodUnit = pet.DefaultFoodUnit,
				DefaultFoodType = pet.DefaultFoodType,
				LastModified = pet.LastModified,
				IsDeleted = pet.IsDeleted
			});
		}

		var request = new SyncRequest
		{
			PetId = petId,
			DeviceUserId = Constants.DeviceUserId,
			LastSyncTimestamp = lastSync,
			Pets = petDtos,
			InsulinLogs = unsyncedInsulin.Select(l => new InsulinLogDto
			{
				Id = l.Id, PetId = l.PetId, DoseIU = l.DoseIU,
				AdministeredAt = l.AdministeredAt, InjectionSite = l.InjectionSite,
				Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified,
				IsDeleted = l.IsDeleted
			}).ToList(),
			FeedingLogs = unsyncedFeeding.Select(l => new FeedingLogDto
			{
				Id = l.Id, PetId = l.PetId, FoodName = l.FoodName,
				Amount = l.Amount, Unit = l.Unit, FoodType = l.FoodType,
				FedAt = l.FedAt, Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified,
				IsDeleted = l.IsDeleted
			}).ToList(),
			WeightLogs = unsyncedWeight.Select(l => new WeightLogDto
			{
				Id = l.Id, PetId = l.PetId, Weight = l.Weight, Unit = l.Unit,
				RecordedAt = l.RecordedAt, Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById, LastModified = l.LastModified,
				IsDeleted = l.IsDeleted
			}).ToList(),
			VetInfos = unsyncedVetInfo.Select(v => new VetInfoDto
			{
				Id = v.Id, PetId = v.PetId, VetName = v.VetName,
				ClinicName = v.ClinicName, Phone = v.Phone,
				EmergencyPhone = v.EmergencyPhone, Address = v.Address,
				Email = v.Email, Notes = v.Notes, LastModified = v.LastModified,
				IsDeleted = v.IsDeleted
			}).ToList(),
			Schedules = unsyncedSchedules.Select(s => new ScheduleDto
			{
				Id = s.Id, PetId = s.PetId, ScheduleType = s.ScheduleType,
				Label = s.Label, TimeTicks = s.TimeTicks,
				IsEnabled = s.IsEnabled,
				ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
				LastModified = s.LastModified,
				IsDeleted = s.IsDeleted
			}).ToList()
		};

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/sync", request, AppJsonSerializerContext.Default.SyncRequest);

		if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
			throw new UnauthorizedAccessException("Access to this pet has been revoked.");

		response.EnsureSuccessStatusCode();

		var syncResponse = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.SyncResponse);
		if (syncResponse is null) return;

		// Apply server changes locally (last-write-wins by LastModified)
		foreach (var p in syncResponse.Pets)
		{
			var local = await _db.GetPetAsync(p.Id);
			if (local is null || p.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new Pet
				{
					Id = p.Id, OwnerId = p.OwnerId, OwnerName = p.OwnerName,
					AccessLevel = p.AccessLevel,
					Name = p.Name, Species = p.Species, Breed = p.Breed,
					DateOfBirth = p.DateOfBirth, InsulinType = p.InsulinType,
					PhotoUrl = p.PhotoUrl,
					InsulinConcentration = p.InsulinConcentration, CurrentDoseIU = p.CurrentDoseIU,
					WeightUnit = p.WeightUnit, CurrentWeight = p.CurrentWeight,
					DefaultFoodName = p.DefaultFoodName,
					DefaultFoodAmount = p.DefaultFoodAmount,
					DefaultFoodUnit = p.DefaultFoodUnit,
					DefaultFoodType = p.DefaultFoodType,
					LastModified = p.LastModified, IsSynced = true,
					IsDeleted = p.IsDeleted
				});
			}
		}

		foreach (var l in syncResponse.InsulinLogs)
		{
			var local = await _db.GetInsulinLogAsync(l.Id);
			if (local is null || l.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new InsulinLog
				{
					Id = l.Id, PetId = l.PetId, DoseIU = l.DoseIU,
					AdministeredAt = l.AdministeredAt, InjectionSite = l.InjectionSite,
					Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById,
					LastModified = l.LastModified, IsSynced = true,
					IsDeleted = l.IsDeleted
				});
			}
		}

		foreach (var l in syncResponse.FeedingLogs)
		{
			var local = await _db.GetFeedingLogAsync(l.Id);
			if (local is null || l.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new FeedingLog
				{
					Id = l.Id, PetId = l.PetId, FoodName = l.FoodName,
					Amount = l.Amount, Unit = l.Unit, FoodType = l.FoodType,
					FedAt = l.FedAt, Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById,
					LastModified = l.LastModified, IsSynced = true,
					IsDeleted = l.IsDeleted
				});
			}
		}

		foreach (var l in syncResponse.WeightLogs)
		{
			var local = await _db.GetWeightLogAsync(l.Id);
			if (local is null || l.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new WeightLog
				{
					Id = l.Id, PetId = l.PetId, Weight = l.Weight,
					Unit = l.Unit, RecordedAt = l.RecordedAt,
					Notes = l.Notes, LoggedBy = l.LoggedBy, LoggedById = l.LoggedById,
					LastModified = l.LastModified, IsSynced = true,
					IsDeleted = l.IsDeleted
				});
			}
		}

		foreach (var v in syncResponse.VetInfos)
		{
			var local = await _db.GetVetInfoByIdAsync(v.Id);
			if (local is null || v.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new VetInfo
				{
					Id = v.Id, PetId = v.PetId, VetName = v.VetName,
					ClinicName = v.ClinicName, Phone = v.Phone,
					EmergencyPhone = v.EmergencyPhone, Address = v.Address,
					Email = v.Email, Notes = v.Notes,
					LastModified = v.LastModified, IsSynced = true,
					IsDeleted = v.IsDeleted
				});
			}
		}

		foreach (var s in syncResponse.Schedules)
		{
			var local = await _db.GetScheduleAsync(s.Id);
			if (local is null || s.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new Schedule
				{
					Id = s.Id, PetId = s.PetId, ScheduleType = s.ScheduleType,
					Label = s.Label, TimeTicks = s.TimeTicks,
					IsEnabled = s.IsEnabled,
					ReminderLeadTimeMinutes = s.ReminderLeadTimeMinutes,
					LastModified = s.LastModified, IsSynced = true,
					IsDeleted = s.IsDeleted
				});
			}
		}

		// Mark local items as synced
		foreach (var p in unsyncedPets) await _db.MarkSyncedAsync<Pet>(p.Id);
		foreach (var l in unsyncedInsulin) await _db.MarkSyncedAsync<InsulinLog>(l.Id);
		foreach (var l in unsyncedFeeding) await _db.MarkSyncedAsync<FeedingLog>(l.Id);
		foreach (var l in unsyncedWeight) await _db.MarkSyncedAsync<WeightLog>(l.Id);
		foreach (var v in unsyncedVetInfo) await _db.MarkSyncedAsync<VetInfo>(v.Id);
		foreach (var s in unsyncedSchedules) await _db.MarkSyncedAsync<Schedule>(s.Id);

		Preferences.Set($"lastSync_{petId}", syncResponse.SyncTimestamp.ToString("O"));
	}

	private static byte[] CreateThumbnailJpeg(string photoPath, int maxSize, int quality)
	{
		try
		{
			using var input = File.OpenRead(photoPath);
			using var original = SKBitmap.Decode(input);
			if (original is null) return [];

			var maxDimension = Math.Max(original.Width, original.Height);
			var scale = maxDimension > maxSize ? (float)maxSize / maxDimension : 1f;
			var targetWidth = Math.Max(1, (int)Math.Round(original.Width * scale));
			var targetHeight = Math.Max(1, (int)Math.Round(original.Height * scale));

			var imageInfo = new SKImageInfo(targetWidth, targetHeight, original.ColorType, original.AlphaType);
			using var resized = new SKBitmap(imageInfo);
			using (var canvas = new SKCanvas(resized))
			{
				canvas.Clear(SKColors.Transparent);
				var destRect = new SKRect(0, 0, targetWidth, targetHeight);
				canvas.DrawBitmap(original, destRect);
			}

			using var image = SKImage.FromBitmap(resized);
			using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
			return data.ToArray();
		}
		catch
		{
			return [];
		}
	}

	public async Task DeleteShareCodeAsync(string shareCode)
	{
		if (Constants.IsOfflineMode) return;

		var response = await _http.DeleteAsync($"{Constants.ApiBaseUrl}/share/{shareCode}");
		response.EnsureSuccessStatusCode();
	}

	public async Task SyncAllAsync()
	{
		if (Constants.IsOfflineMode) return;

		var pets = await _db.GetPetsAsync();
		var exceptions = new List<Exception>();
		var syncTasks = pets.Select(async p =>
		{
			try
			{
				await SyncAsync(p.Id);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Sync failed for pet {p.Id}: {ex.Message}");
				lock (exceptions) exceptions.Add(ex);
			}
		});
		await Task.WhenAll(syncTasks);

		if (exceptions.Count > 0)
			throw new AggregateException("One or more pet syncs failed.", exceptions);
	}
}
