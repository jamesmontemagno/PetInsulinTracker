using PetInsulinTracker.Models;

namespace PetInsulinTracker.Services;

public interface INotificationService
{
	Task ScheduleNotificationsForPetAsync(string petId);
	Task CancelNotificationsForPetAsync(string petId);
	Task RescheduleAllAsync();
}
