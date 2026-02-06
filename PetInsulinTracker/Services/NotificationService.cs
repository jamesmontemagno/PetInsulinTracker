using PetInsulinTracker.Models;
using Plugin.LocalNotification;

namespace PetInsulinTracker.Services;

public class NotificationService : INotificationService
{
	private readonly IDatabaseService _db;

	public NotificationService(IDatabaseService db)
	{
		_db = db;
	}

	public async Task ScheduleNotificationsForPetAsync(string petId)
	{
		await CancelNotificationsForPetAsync(petId);

		var schedules = await _db.GetSchedulesAsync(petId);
		var pet = await _db.GetPetAsync(petId);
		if (pet is null) return;

		foreach (var schedule in schedules.Where(s => s.IsEnabled))
		{
			var notificationId = GenerateNotificationId(petId, schedule.Id);
			var notifyTime = DateTime.Today.Add(schedule.TimeOfDay)
				.AddMinutes(-schedule.ReminderLeadTimeMinutes);

			// If the time has passed today, schedule for tomorrow
			if (notifyTime < DateTime.Now)
				notifyTime = notifyTime.AddDays(1);

			var emoji = schedule.ScheduleType == "Insulin" ? "💉" : "🍽️";
			var request = new NotificationRequest
			{
				NotificationId = notificationId,
				Title = $"{emoji} {schedule.Label}",
				Description = $"Time for {pet.Name}'s {schedule.ScheduleType.ToLowerInvariant()}!",
				Schedule = new NotificationRequestSchedule
				{
					NotifyTime = notifyTime,
					RepeatType = NotificationRepeat.Daily
				}
			};

			await LocalNotificationCenter.Current.Show(request);
		}
	}

	public async Task CancelNotificationsForPetAsync(string petId)
	{
		var schedules = await _db.GetSchedulesAsync(petId);
		foreach (var schedule in schedules)
		{
			var notificationId = GenerateNotificationId(petId, schedule.Id);
			LocalNotificationCenter.Current.Cancel(notificationId);
		}
	}

	public async Task RescheduleAllAsync()
	{
		LocalNotificationCenter.Current.CancelAll();
		var pets = await _db.GetPetsAsync();
		foreach (var pet in pets)
		{
			await ScheduleNotificationsForPetAsync(pet.Id);
		}
	}

	// Generate a stable int ID from pet+schedule string IDs
	private static int GenerateNotificationId(string petId, string scheduleId)
	{
		return HashCode.Combine(petId, scheduleId);
	}
}
