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
	private static readonly char[] ShareCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

	public PetFunctions(ILogger<PetFunctions> logger, TableStorageService storage)
	{
		_logger = logger;
		_storage = storage;
	}

	[Function("CreatePet")]
	public async Task<HttpResponseData> CreatePet(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pets")] HttpRequestData req)
	{
		var request = await req.ReadFromJsonAsync<CreatePetRequest>();
		if (request is null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.DeviceUserId))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var petId = string.IsNullOrEmpty(request.Id) ? Guid.NewGuid().ToString() : request.Id;

		// Generate a unique 6-character share code
		string code;
		do
		{
			code = GenerateCode(6);
		}
		while (await _storage.GetShareCodeAsync(code) is not null);

		var now = DateTimeOffset.UtcNow;

		// Create the pet entity partitioned by share code
		var petEntity = new PetEntity
		{
			RowKey = petId,
			OwnerId = request.DeviceUserId,
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
			ShareCode = code,
			LastModified = now,
			IsDeleted = false
		};
		await _storage.UpsertPetAsync(code, petEntity);

		// Create the share code entity with the owner recorded
		await _storage.CreateShareCodeAsync(code, petId, "full", request.DeviceUserId);

		_logger.LogInformation("Created pet {PetId} with share code {Code} for owner {OwnerId}", petId, code, request.DeviceUserId);

		var result = new CreatePetResponse
		{
			Pet = new PetDto
			{
				Id = petId,
				OwnerId = request.DeviceUserId,
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
				ShareCode = code,
				LastModified = now
			},
			ShareCode = code
		};

		var response = req.CreateResponse(HttpStatusCode.Created);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	private static string GenerateCode(int length)
	{
		var random = Random.Shared;
		return new string(Enumerable.Range(0, length)
			.Select(_ => ShareCodeChars[random.Next(ShareCodeChars.Length)])
			.ToArray());
	}
}
