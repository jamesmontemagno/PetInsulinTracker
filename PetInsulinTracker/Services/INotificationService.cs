using PetInsulinTracker.Models;

namespace PetInsulinTracker.Services;

public interface INotificationService
{
	Task<bool> EnsurePermissionAsync();
	Task ScheduleNotificationsForPetAsync(string petId);
	Task CancelNotificationsForPetAsync(string petId);
	Task RescheduleAllAsync();
	Task CancelAllAsync();
}
