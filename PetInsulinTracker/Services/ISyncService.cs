namespace PetInsulinTracker.Services;

public interface ISyncService
{
	Task<string> GenerateShareCodeAsync(string petId);
	Task RedeemShareCodeAsync(string shareCode);
	Task SyncAsync(string shareCode);
	Task SyncAllAsync();
}
