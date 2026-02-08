namespace PetInsulinTracker.Services;

public interface ISyncService
{
	Task CreatePetAsync(PetInsulinTracker.Models.Pet pet);
	Task<string> GenerateShareCodeAsync(string petId, string accessLevel = "full");
	Task RedeemShareCodeAsync(string shareCode);
	Task<List<PetInsulinTracker.Shared.DTOs.SharedUserDto>> GetSharedUsersAsync(string shareCode);
	Task RevokeAccessAsync(string shareCode, string deviceUserId);
	Task SyncAsync(string petId);
	Task DeleteShareCodeAsync(string shareCode);
	Task SyncAllAsync();
}
