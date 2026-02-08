using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PetInsulinTracker.Api.Services;

public class BlobStorageService
{
	private readonly BlobContainerClient _container;
	private readonly SemaphoreSlim _initLock = new(1, 1);
	private bool _initialized;

	public BlobStorageService()
	{
		var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString")
			?? "UseDevelopmentStorage=true";
		var containerName = Environment.GetEnvironmentVariable("BlobContainerName") ?? "pet-photos";
		var serviceClient = new BlobServiceClient(connectionString);
		_container = serviceClient.GetBlobContainerClient(containerName);
	}

	public async Task<string> UploadPetThumbnailAsync(string petId, byte[] bytes)
	{
		await EnsureInitializedAsync();

		var blobName = $"thumbnails/{petId}.jpg";
		var blob = _container.GetBlobClient(blobName);
		using var stream = new MemoryStream(bytes);
		await blob.UploadAsync(stream, new BlobUploadOptions
		{
			HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
		}, cancellationToken: default);
		return blob.Uri.ToString();
	}

	public async Task DeletePetThumbnailAsync(string petId)
	{
		await EnsureInitializedAsync();

		var blobName = $"thumbnails/{petId}.jpg";
		var blob = _container.GetBlobClient(blobName);
		await blob.DeleteIfExistsAsync();
	}

	private async Task EnsureInitializedAsync()
	{
		if (_initialized) return;
		await _initLock.WaitAsync();
		try
		{
			if (_initialized) return;
			await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);
			_initialized = true;
		}
		finally
		{
			_initLock.Release();
		}
	}
}
