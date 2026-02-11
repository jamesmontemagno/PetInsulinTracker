using System.Security.Cryptography;
using PetInsulinTracker.Helpers;
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

	public async Task<bool> EnsurePermissionAsync()
	{
		if (!LocalNotificationCenter.Current.IsSupported)
			return false;

		if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
			return true;

		return await LocalNotificationCenter.Current.RequestNotificationPermission();
	}

	public async Task ScheduleNotificationsForPetAsync(string petId)
	{
		if (!Preferences.Get(Constants.NotificationsEnabledKey, true))
			return;

		if (!Preferences.Get(Constants.GetPetNotificationsKey(petId), true))
		{
			await CancelNotificationsForPetAsync(petId);
			return;
		}

		await CancelNotificationsForPetAsync(petId);

		var schedules = await _db.GetSchedulesAsync(petId);
		var pet = await _db.GetPetAsync(petId);
		if (pet is null) return;

		foreach (var schedule in schedules)
		{
			var notificationId = GenerateNotificationId(petId, schedule.Id);
			var notifyTime = DateTime.Today.Add(schedule.TimeOfDay)
				.AddMinutes(-schedule.ReminderLeadTimeMinutes);

			// If the time has passed today, schedule for tomorrow
			if (notifyTime < DateTime.Now)
				notifyTime = notifyTime.AddDays(1);

			var emoji = schedule.ScheduleType switch
			{
				Constants.ScheduleTypeInsulin => "💉",
				Constants.ScheduleTypeFeeding => "🍽️",
				Constants.ScheduleTypeCombined => "💉🍽️",
				Constants.ScheduleTypeMedication => "💊",
				_ => "⏰"
			};
			var scheduleText = schedule.ScheduleType switch
			{
				Constants.ScheduleTypeCombined => "insulin and feeding",
				Constants.ScheduleTypeMedication => "medication",
				_ => schedule.ScheduleType.ToLowerInvariant()
			};
			var request = new NotificationRequest
			{
				NotificationId = notificationId,
				Title = $"{emoji} {schedule.Label}",
				Description = $"Time for {pet.Name}'s {scheduleText}!",
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
		if (!Preferences.Get(Constants.NotificationsEnabledKey, true))
		{
			LocalNotificationCenter.Current.CancelAll();
			return;
		}

		LocalNotificationCenter.Current.CancelAll();
		var pets = await _db.GetPetsAsync();
		foreach (var pet in pets)
		{
			if (!Preferences.Get(Constants.GetPetNotificationsKey(pet.Id), true))
				continue;
			await ScheduleNotificationsForPetAsync(pet.Id);
		}
	}

	public Task CancelAllAsync()
	{
		LocalNotificationCenter.Current.CancelAll();
		return Task.CompletedTask;
	}

	// Generate a stable int ID from pet+schedule string IDs
	private static int GenerateNotificationId(string petId, string scheduleId)
	{
		var input = System.Text.Encoding.UTF8.GetBytes($"{petId}:{scheduleId}");
		var hash = SHA256.HashData(input);
		var value = BitConverter.ToInt32(hash, 0);
		return value & int.MaxValue;
	}
}
