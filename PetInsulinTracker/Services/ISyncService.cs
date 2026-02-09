namespace PetInsulinTracker.Services;

public interface ISyncService
{
	Task CreatePetAsync(PetInsulinTracker.Models.Pet pet);
	Task<string> GenerateShareCodeAsync(string petId, string accessLevel = "full");
	Task RedeemShareCodeAsync(string shareCode);
	Task<List<PetInsulinTracker.Shared.DTOs.ShareCodeDto>> GetShareCodesAsync(string petId);
	Task<List<PetInsulinTracker.Shared.DTOs.SharedUserDto>> GetSharedUsersAsync(string petId);
	Task RevokeAccessAsync(string petId, string deviceUserId);
	Task LeavePetAsync(string petId);
	Task DeletePetAsync(string petId);
	Task SyncAsync(string petId);
	Task<string?> UploadPetPhotoThumbnailAsync(string petId, string photoPath);
	Task DeleteShareCodeAsync(string shareCode);
	Task SyncAllAsync();
}
