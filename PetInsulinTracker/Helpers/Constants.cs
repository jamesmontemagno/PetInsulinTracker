namespace PetInsulinTracker.Helpers;

public static class Constants
{
	// Azure Functions API base URL — update when deployed
	public const string ApiBaseUrl = "https://localhost:7071/api";

	public const string OwnerNameKey = "owner_name";
	public const string DeviceUserIdKey = "device_user_id";

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
