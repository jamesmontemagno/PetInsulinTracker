using PetInsulinTracker.Models;

namespace PetInsulinTracker.Services;

public interface IDatabaseService
{
	// Pets
	Task<List<Pet>> GetPetsAsync();
	Task<Pet?> GetPetAsync(string id);
	Task<int> SavePetAsync(Pet pet);
	Task<int> DeletePetAsync(Pet pet);

	// Insulin Logs
	Task<List<InsulinLog>> GetInsulinLogsAsync(string petId);
	Task<InsulinLog?> GetLatestInsulinLogAsync(string petId);
	Task<int> SaveInsulinLogAsync(InsulinLog log);
	Task<int> DeleteInsulinLogAsync(InsulinLog log);

	// Feeding Logs
	Task<List<FeedingLog>> GetFeedingLogsAsync(string petId);
	Task<int> SaveFeedingLogAsync(FeedingLog log);
	Task<int> DeleteFeedingLogAsync(FeedingLog log);

	// Weight Logs
	Task<List<WeightLog>> GetWeightLogsAsync(string petId);
	Task<WeightLog?> GetLatestWeightLogAsync(string petId);
	Task<int> SaveWeightLogAsync(WeightLog log);
	Task<int> DeleteWeightLogAsync(WeightLog log);

	// Vet Info
	Task<VetInfo?> GetVetInfoAsync(string petId);
	Task<int> SaveVetInfoAsync(VetInfo info);

	// Schedules
	Task<List<Schedule>> GetSchedulesAsync(string petId);
	Task<List<Schedule>> GetAllEnabledSchedulesAsync();
	Task<int> SaveScheduleAsync(Schedule schedule);
	Task<int> DeleteScheduleAsync(Schedule schedule);

	// Sync support
	Task<List<T>> GetUnsyncedAsync<T>() where T : new();
	Task MarkSyncedAsync<T>(string id) where T : new();
}
