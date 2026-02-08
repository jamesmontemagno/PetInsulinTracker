using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PetInsulinTracker.Api.Services;
using PetInsulinTracker.Shared.DTOs;

namespace PetInsulinTracker.Api.Functions;

public class PetPhotoFunctions
{
	private readonly ILogger<PetPhotoFunctions> _logger;
	private readonly TableStorageService _storage;
	private readonly BlobStorageService _blob;

	public PetPhotoFunctions(ILogger<PetPhotoFunctions> logger, TableStorageService storage, BlobStorageService blob)
	{
		_logger = logger;
		_storage = storage;
		_blob = blob;
	}

	[Function("UploadPetPhotoThumbnail")]
	public async Task<HttpResponseData> UploadPetPhotoThumbnail(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "pets/{petId}/photo-thumbnail")] HttpRequestData req,
		string petId)
	{
		PetPhotoUploadRequest? request;
		try
		{
			request = await req.ReadFromJsonAsync<PetPhotoUploadRequest>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize pet photo upload request");
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (request is null || string.IsNullOrEmpty(request.DeviceUserId) || string.IsNullOrEmpty(request.Base64Image))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (!string.Equals(request.PetId, petId, StringComparison.Ordinal))
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		var pet = await _storage.GetPetAsync(petId);
		if (pet is null || pet.IsDeleted)
			return req.CreateResponse(HttpStatusCode.NotFound);

		var isOwner = string.Equals(pet.OwnerId, request.DeviceUserId, StringComparison.Ordinal);
		if (!isOwner)
		{
			var redemption = await _storage.GetRedemptionAsync(petId, request.DeviceUserId);
			if (redemption is null || redemption.IsRevoked || redemption.AccessLevel == "guest")
				return req.CreateResponse(HttpStatusCode.Forbidden);
		}

		byte[] bytes;
		try
		{
			bytes = Convert.FromBase64String(request.Base64Image);
		}
		catch
		{
			return req.CreateResponse(HttpStatusCode.BadRequest);
		}

		if (bytes.Length > 512 * 1024)
			return req.CreateResponse(HttpStatusCode.BadRequest);

		var url = await _blob.UploadPetThumbnailAsync(petId, bytes);
		pet.PhotoUrl = url;
		pet.LastModified = DateTimeOffset.UtcNow;
		await _storage.UpsertPetAsync(pet);

		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(new PetPhotoUploadResponse { PhotoUrl = url });
		return response;
	}
}
