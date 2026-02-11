using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PetInsulinTracker.Api.Models;
using PetInsulinTracker.Api.Services;
using PetInsulinTracker.Shared.DTOs;

namespace PetInsulinTracker.Api.Functions;

public class PetFunctions
{
	private readonly ILogger<PetFunctions> _logger;
	private readonly TableStorageService _storage;
	private readonly BlobStorageService _blob;

	public PetFunctions(ILogger<PetFunctions> logger, TableStorageService storage, BlobStorageService blob)
	{
		_logger = logger;
		_storage = storage;
		_blob = blob;
	}

	private static DateTime? EnsureUtc(DateTime? dt) =>
		dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

	[Function("CreatePet")]
	public async Task<HttpResponseData> CreatePet(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pets")] HttpRequestData req)
	{
		CreatePetRequest? request;
		try
		{
			request = await req.ReadFromJsonAsync<CreatePetRequest>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize CreatePet request body");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (request is null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.DeviceUserId))
		{
			_logger.LogWarning("CreatePet rejected: missing Name or DeviceUserId");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		_logger.LogInformation("CreatePet request: Name={Name}, DeviceUserId={DeviceUserId}", request.Name, request.DeviceUserId);
		var petId = string.IsNullOrEmpty(request.Id) ? Guid.NewGuid().ToString() : request.Id;
		var now = DateTimeOffset.UtcNow;

		var petEntity = new PetEntity
		{
			RowKey = petId,
			OwnerId = request.DeviceUserId,
			OwnerName = request.OwnerName,
			AccessLevel = "owner",
			Name = request.Name,
			Species = request.Species,
			Breed = request.Breed,
			DateOfBirth = EnsureUtc(request.DateOfBirth),
			InsulinType = request.InsulinType,
			InsulinConcentration = request.InsulinConcentration,
			CurrentDoseIU = request.CurrentDoseIU,
			WeightUnit = request.WeightUnit,
			CurrentWeight = request.CurrentWeight,
			DefaultFoodName = request.DefaultFoodName,
			DefaultFoodAmount = request.DefaultFoodAmount,
			DefaultFoodUnit = request.DefaultFoodUnit,
			DefaultFoodType = request.DefaultFoodType,
			PetMedication = request.PetMedication,
			LastModified = now,
			IsDeleted = false
		};
		await _storage.UpsertPetAsync(petEntity);

		_logger.LogInformation("Created pet {PetId} for owner {OwnerId}", petId, request.DeviceUserId);

		var result = new CreatePetResponse
		{
			Pet = new PetDto
			{
				Id = petId,
				OwnerId = request.DeviceUserId,
				OwnerName = request.OwnerName,
				AccessLevel = "owner",
				Name = request.Name,
				Species = request.Species,
				Breed = request.Breed,
				DateOfBirth = request.DateOfBirth,
				PhotoUrl = petEntity.PhotoUrl,
				InsulinType = request.InsulinType,
				InsulinConcentration = request.InsulinConcentration,
				CurrentDoseIU = request.CurrentDoseIU,
				WeightUnit = request.WeightUnit,
				CurrentWeight = request.CurrentWeight,
				DefaultFoodName = request.DefaultFoodName,
				DefaultFoodAmount = request.DefaultFoodAmount,
				DefaultFoodUnit = request.DefaultFoodUnit,
				DefaultFoodType = request.DefaultFoodType,
				PetMedication = request.PetMedication,
				LastModified = now
			}
		};

		var response = req.CreateResponse(HttpStatusCode.Created);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	[Function("DeletePet")]
	public async Task<HttpResponseData> DeletePet(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pets/delete")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<DeletePetRequest>();
		if (request is null || string.IsNullOrEmpty(request.PetId) || string.IsNullOrEmpty(request.OwnerId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var pet = await _storage.GetPetAsync(request.PetId);
		if (pet is null)
			return req.CreateResponse(HttpStatusCode.NotFound);

		if (!string.Equals(pet.OwnerId, request.OwnerId, StringComparison.Ordinal))
			return req.CreateResponse(HttpStatusCode.Forbidden);

		var deleted = await _storage.DeletePetAsync(request.PetId);
		if (!deleted)
			return req.CreateResponse(HttpStatusCode.NotFound);

		// Clean up blob storage thumbnail
		try
		{
			await _blob.DeletePetThumbnailAsync(request.PetId);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to delete thumbnail for pet {PetId}", request.PetId);
		}

		_logger.LogInformation("Deleted pet {PetId} by owner {OwnerId}", request.PetId, request.OwnerId);
		return req.CreateResponse(HttpStatusCode.OK);
	}
}
