using System.Text.Json.Serialization;
using PetInsulinTracker.Shared.DTOs;

namespace PetInsulinTracker.Shared;

/// <summary>
/// JSON source generator context for AOT-compatible serialization.
/// This eliminates reflection-based JSON serialization which is incompatible with iOS AOT.
/// </summary>
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	WriteIndented = false)]
// DTOs
[JsonSerializable(typeof(PetDto))]
[JsonSerializable(typeof(InsulinLogDto))]
[JsonSerializable(typeof(FeedingLogDto))]
[JsonSerializable(typeof(WeightLogDto))]
[JsonSerializable(typeof(MedicationLogDto))]
[JsonSerializable(typeof(VetInfoDto))]
[JsonSerializable(typeof(ScheduleDto))]
// Sync DTOs
[JsonSerializable(typeof(SyncRequest))]
[JsonSerializable(typeof(SyncResponse))]
[JsonSerializable(typeof(ShareCodeRequest))]
[JsonSerializable(typeof(ShareCodeResponse))]
[JsonSerializable(typeof(ShareCodeDto))]
[JsonSerializable(typeof(ShareCodesResponse))]
[JsonSerializable(typeof(RedeemShareCodeRequest))]
[JsonSerializable(typeof(RedeemShareCodeResponse))]
[JsonSerializable(typeof(SharedUserDto))]
[JsonSerializable(typeof(SharedUsersResponse))]
[JsonSerializable(typeof(RevokeAccessRequest))]
[JsonSerializable(typeof(LeavePetRequest))]
[JsonSerializable(typeof(DeletePetRequest))]
[JsonSerializable(typeof(CreatePetRequest))]
[JsonSerializable(typeof(CreatePetResponse))]
[JsonSerializable(typeof(PetPhotoUploadRequest))]
[JsonSerializable(typeof(PetPhotoUploadResponse))]
// Collections
[JsonSerializable(typeof(List<PetDto>))]
[JsonSerializable(typeof(List<InsulinLogDto>))]
[JsonSerializable(typeof(List<FeedingLogDto>))]
[JsonSerializable(typeof(List<WeightLogDto>))]
[JsonSerializable(typeof(List<MedicationLogDto>))]
[JsonSerializable(typeof(List<VetInfoDto>))]
[JsonSerializable(typeof(List<ScheduleDto>))]
[JsonSerializable(typeof(List<ShareCodeDto>))]
[JsonSerializable(typeof(List<SharedUserDto>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
