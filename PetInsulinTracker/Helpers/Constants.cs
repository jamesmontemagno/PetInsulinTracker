namespace PetInsulinTracker.Helpers;

public static class Constants
{
	// Azure Functions API base URL — update when deployed
	public const string ApiBaseUrl = "https://localhost:7071/api";

	public const string OwnerNameKey = "owner_name";

	/// <summary>Gets the current owner name from preferences.</summary>
	public static string OwnerName => Preferences.Get(OwnerNameKey, string.Empty);
}
