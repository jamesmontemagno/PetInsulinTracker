using System.Net.Http.Json;
using PetInsulinTracker.Helpers;
using PetInsulinTracker.Models;
using PetInsulinTracker.Shared.DTOs;

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

	public async Task<string> CreatePetAsync(Pet pet)
	{
		var request = new CreatePetRequest
		{
			Id = pet.Id,
			DeviceUserId = Constants.DeviceUserId,
			Name = pet.Name,
			Species = pet.Species,
			Breed = pet.Breed,
			DateOfBirth = pet.DateOfBirth,
			InsulinType = pet.InsulinType,
			InsulinConcentration = pet.InsulinConcentration,
			CurrentDoseIU = pet.CurrentDoseIU,
			WeightUnit = pet.WeightUnit,
			CurrentWeight = pet.CurrentWeight
		};

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/pets", request);
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync<CreatePetResponse>();
		return result?.ShareCode ?? throw new InvalidOperationException("No share code received");
	}

	public async Task<string> GenerateShareCodeAsync(string petId, string accessLevel = "full")
	{
		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/share/generate",
			new ShareCodeRequest { PetId = petId, AccessLevel = accessLevel, OwnerId = Constants.DeviceUserId });

		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync<ShareCodeResponse>();
		return result?.ShareCode ?? throw new InvalidOperationException("No share code received");
	}

	public async Task RedeemShareCodeAsync(string shareCode)
	{
		var request = new RedeemShareCodeRequest
		{
			ShareCode = shareCode,
			DeviceUserId = Constants.DeviceUserId,
			DisplayName = Constants.OwnerName
		};

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/share/redeem", request);
		response.EnsureSuccessStatusCode();

		var data = await response.Content.ReadFromJsonAsync<RedeemShareCodeResponse>();
		if (data is null) throw new InvalidOperationException("No data received");

		// Import pet
		var pet = new Pet
		{
			Id = data.Pet.Id,
			OwnerId = data.Pet.OwnerId,
			AccessLevel = data.Pet.AccessLevel,
			Name = data.Pet.Name,
			Species = data.Pet.Species,
			Breed = data.Pet.Breed,
			DateOfBirth = data.Pet.DateOfBirth,
			InsulinType = data.Pet.InsulinType,
			InsulinConcentration = data.Pet.InsulinConcentration,
			CurrentDoseIU = data.Pet.CurrentDoseIU,
			WeightUnit = data.Pet.WeightUnit,
			CurrentWeight = data.Pet.CurrentWeight,
			ShareCode = shareCode,
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

	public async Task<List<SharedUserDto>> GetSharedUsersAsync(string shareCode)
	{
		var response = await _http.GetAsync($"{Constants.ApiBaseUrl}/share/{shareCode}/users");
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadFromJsonAsync<SharedUsersResponse>();
		return result?.Users ?? [];
	}

	public async Task RevokeAccessAsync(string shareCode, string deviceUserId)
	{
		var response = await _http.PostAsJsonAsync(
			$"{Constants.ApiBaseUrl}/share/revoke",
			new RevokeAccessRequest { ShareCode = shareCode, DeviceUserId = deviceUserId });
		response.EnsureSuccessStatusCode();
	}

	public async Task SyncAsync(string shareCode)
	{
		var pet = await _db.GetPetByShareCodeAsync(shareCode);
		if (pet is null) return;

		var lastSync = Preferences.Get($"lastSync_{shareCode}", DateTimeOffset.MinValue);

		// Gather unsynced local data scoped to this pet
		var unsyncedPets = await _db.GetUnsyncedAsync<Pet>(pet.Id);
		var unsyncedInsulin = await _db.GetUnsyncedAsync<InsulinLog>(pet.Id);
		var unsyncedFeeding = await _db.GetUnsyncedAsync<FeedingLog>(pet.Id);
		var unsyncedWeight = await _db.GetUnsyncedAsync<WeightLog>(pet.Id);
		var unsyncedVetInfo = await _db.GetUnsyncedAsync<VetInfo>(pet.Id);
		var unsyncedSchedules = await _db.GetUnsyncedAsync<Schedule>(pet.Id);

		var request = new SyncRequest
		{
			ShareCode = shareCode,
			DeviceUserId = Constants.DeviceUserId,
			LastSyncTimestamp = lastSync,
			Pets = unsyncedPets.Select(p => new PetDto
			{
				Id = p.Id, OwnerId = p.OwnerId, AccessLevel = p.AccessLevel,
				Name = p.Name, Species = p.Species, Breed = p.Breed,
				DateOfBirth = p.DateOfBirth, InsulinType = p.InsulinType,
				InsulinConcentration = p.InsulinConcentration, CurrentDoseIU = p.CurrentDoseIU,
				WeightUnit = p.WeightUnit, CurrentWeight = p.CurrentWeight,
				ShareCode = p.ShareCode, LastModified = p.LastModified,
				IsDeleted = p.IsDeleted
			}).ToList(),
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

		var response = await _http.PostAsJsonAsync($"{Constants.ApiBaseUrl}/sync", request);

		if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
			throw new UnauthorizedAccessException("Access to this pet has been revoked.");

		response.EnsureSuccessStatusCode();

		var syncResponse = await response.Content.ReadFromJsonAsync<SyncResponse>();
		if (syncResponse is null) return;

		// Apply server changes locally (last-write-wins by LastModified)
		foreach (var p in syncResponse.Pets)
		{
			var local = await _db.GetPetAsync(p.Id);
			if (local is null || p.LastModified > local.LastModified)
			{
				await _db.SaveSyncedAsync(new Pet
				{
					Id = p.Id, OwnerId = p.OwnerId, AccessLevel = p.AccessLevel,
					Name = p.Name, Species = p.Species, Breed = p.Breed,
					DateOfBirth = p.DateOfBirth, InsulinType = p.InsulinType,
					InsulinConcentration = p.InsulinConcentration, CurrentDoseIU = p.CurrentDoseIU,
					WeightUnit = p.WeightUnit, CurrentWeight = p.CurrentWeight,
					ShareCode = p.ShareCode, LastModified = p.LastModified, IsSynced = true,
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

		Preferences.Set($"lastSync_{shareCode}", syncResponse.SyncTimestamp);
	}

	public async Task DeleteShareCodeAsync(string shareCode)
	{
		var response = await _http.DeleteAsync($"{Constants.ApiBaseUrl}/share/{shareCode}");
		response.EnsureSuccessStatusCode();
	}

	public async Task SyncAllAsync()
	{
		var pets = await _db.GetPetsAsync();
		var syncTasks = pets
			.SelectMany(p => new[] { p.ShareCode, p.FullAccessCode, p.GuestAccessCode })
			.Where(code => !string.IsNullOrEmpty(code))
			.Distinct()
			.Select(async code =>
			{
				try
				{
					await SyncAsync(code!);
				}
				catch
				{
					// Silently fail for offline scenarios
				}
			});
		await Task.WhenAll(syncTasks);
	}
}
