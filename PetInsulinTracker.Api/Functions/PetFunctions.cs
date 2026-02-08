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

	public PetFunctions(ILogger<PetFunctions> logger, TableStorageService storage)
	{
		_logger = logger;
		_storage = storage;
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
				InsulinType = request.InsulinType,
				InsulinConcentration = request.InsulinConcentration,
				CurrentDoseIU = request.CurrentDoseIU,
				WeightUnit = request.WeightUnit,
				CurrentWeight = request.CurrentWeight,
				LastModified = now
			}
		};

		var response = req.CreateResponse(HttpStatusCode.Created);
		await response.WriteAsJsonAsync(result);
		return response;
	}
}
