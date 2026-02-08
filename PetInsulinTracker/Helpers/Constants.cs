namespace PetInsulinTracker.Helpers;

public static class Constants
{
	// Azure Functions API base URL — update when deployed
	public const string ApiBaseUrl = "https://localhost:7071/api";

	public const string OwnerNameKey = "owner_name";
	public const string DeviceUserIdKey = "device_user_id";
	public const string OfflineModeKey = "offline_mode";
	public const string NotificationsEnabledKey = "notifications_enabled";

	/// <summary>Whether the app is in fully offline mode (no cloud sync).</summary>
	public static bool IsOfflineMode => Preferences.Get(OfflineModeKey, false);

	/// <summary>Gets the current owner name from preferences.</summary>
	public static string OwnerName => Preferences.Get(OwnerNameKey, string.Empty);

	/// <summary>Gets a stable unique ID for this device/user, generating one if needed.</summary>
	public static string DeviceUserId
	{
		get
		{
			var id = Preferences.Get(DeviceUserIdKey, string.Empty);
			if (string.IsNullOrEmpty(id))
			{
				id = Guid.NewGuid().ToString();
				Preferences.Set(DeviceUserIdKey, id);
			}
			return id;
		}
	}
}
