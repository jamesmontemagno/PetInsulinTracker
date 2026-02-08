namespace PetInsulinTracker.Services;

public interface ISyncService
{
	Task<string> CreatePetAsync(PetInsulinTracker.Models.Pet pet);
	Task<string> GenerateShareCodeAsync(string petId, string accessLevel = "full");
	Task RedeemShareCodeAsync(string shareCode);
	Task<List<PetInsulinTracker.Shared.DTOs.SharedUserDto>> GetSharedUsersAsync(string shareCode);
	Task RevokeAccessAsync(string shareCode, string deviceUserId);
	Task SyncAsync(string shareCode);
	Task DeleteShareCodeAsync(string shareCode);
	Task SyncAllAsync();
}
