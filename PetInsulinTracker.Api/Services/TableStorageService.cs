using Azure.Data.Tables;
using PetInsulinTracker.Api.Models;

namespace PetInsulinTracker.Api.Services;

public class TableStorageService
{
	private readonly TableServiceClient _serviceClient;
	private readonly HashSet<string> _createdTables = [];

	/// <summary>Azure Table Storage minimum supported date.</summary>
	private static readonly DateTimeOffset MinTableDate = new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);

	private static DateTimeOffset ClampDate(DateTimeOffset d) => d < MinTableDate ? MinTableDate : d;

	public TableStorageService()
	{
		var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString")
			?? "UseDevelopmentStorage=true";
		_serviceClient = new TableServiceClient(connectionString);
	}

	private async Task<TableClient> GetTableClientAsync(string tableName)
	{
		var client = _serviceClient.GetTableClient(tableName);
		if (_createdTables.Add(tableName))
		{
			await client.CreateIfNotExistsAsync();
		}
		return client;
	}

	// Pets — partitioned by OwnerId, RowKey = PetId
	public async Task UpsertPetAsync(PetEntity entity)
	{
		var client = await GetTableClientAsync("Pets");
		entity.RowKey = entity.RowKey.Length > 0 ? entity.RowKey : Guid.NewGuid().ToString();
		entity.PartitionKey = entity.OwnerId ?? entity.RowKey;
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	public async Task<PetEntity?> GetPetAsync(string petId)
	{
		var client = await GetTableClientAsync("Pets");
		// Query by RowKey since we may not know the OwnerId
		await foreach (var entity in client.QueryAsync<PetEntity>(e => e.RowKey == petId))
		{
			return entity;
		}
		return null;
	}

	public async Task<List<PetEntity>> GetPetsByOwnerAsync(string ownerId)
	{
		var client = await GetTableClientAsync("Pets");
		var results = new List<PetEntity>();
		await foreach (var entity in client.QueryAsync<PetEntity>(e => e.PartitionKey == ownerId))
		{
			results.Add(entity);
		}
		return results;
	}

	// Generic log operations
	public async Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
	{
		var client = await GetTableClientAsync(tableName);
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	public async Task<List<T>> GetEntitiesByPartitionAsync<T>(string tableName, string partitionKey) where T : class, ITableEntity, new()
	{
		var client = await GetTableClientAsync(tableName);
		var results = new List<T>();
		await foreach (var entity in client.QueryAsync<T>(e => e.PartitionKey == partitionKey))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task<List<T>> GetEntitiesModifiedSinceAsync<T>(string tableName, string partitionKey, DateTimeOffset since) where T : class, ITableEntity, new()
	{
		var client = await GetTableClientAsync(tableName);
		var clampedSince = ClampDate(since);
		var results = new List<T>();
		await foreach (var entity in client.QueryAsync<T>(
			e => e.PartitionKey == partitionKey && e.Timestamp >= clampedSince))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task<List<PetEntity>> GetPetsModifiedSinceAsync(string petId, DateTimeOffset since)
	{
		var client = await GetTableClientAsync("Pets");
		var clampedSince = ClampDate(since);
		var results = new List<PetEntity>();
		await foreach (var entity in client.QueryAsync<PetEntity>(
			e => e.RowKey == petId && e.Timestamp >= clampedSince))
		{
			results.Add(entity);
		}
		return results;
	}

	// Share codes — PK = PetId, RK = Code
	public async Task<ShareCodeEntity?> GetShareCodeAsync(string code)
	{
		var client = await GetTableClientAsync("ShareCodes");
		// Code is globally unique; cross-partition scan by RK (infrequent: redemption only)
		await foreach (var entity in client.QueryAsync<ShareCodeEntity>(e => e.RowKey == code))
		{
			return entity;
		}
		return null;
	}

	public async Task<bool> DeleteShareCodeAsync(string code)
	{
		var entity = await GetShareCodeAsync(code);
		if (entity is null) return false;

		var client = await GetTableClientAsync("ShareCodes");
		try
		{
			await client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
			return true;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return false;
		}
	}

	public async Task<List<ShareCodeEntity>> GetShareCodesByPetIdAsync(string petId)
	{
		var client = await GetTableClientAsync("ShareCodes");
		var results = new List<ShareCodeEntity>();
		await foreach (var entity in client.QueryAsync<ShareCodeEntity>(e => e.PartitionKey == petId))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task CreateShareCodeAsync(string code, string petId, string accessLevel = "full", string? ownerId = null)
	{
		var client = await GetTableClientAsync("ShareCodes");
		var entity = new ShareCodeEntity
		{
			PartitionKey = petId,
			RowKey = code,
			PetId = petId,
			AccessLevel = accessLevel,
			OwnerId = ownerId
		};
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	// Share redemptions — PK = PetId, RK = DeviceUserId
	public async Task CreateRedemptionAsync(string petId, string shareCode, string deviceUserId, string displayName, string accessLevel)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		var entity = new ShareRedemptionEntity
		{
			PartitionKey = petId,
			RowKey = deviceUserId,
			ShareCode = shareCode,
			DisplayName = displayName,
			AccessLevel = accessLevel
		};
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	public async Task<List<ShareRedemptionEntity>> GetRedemptionsByPetAsync(string petId)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		var results = new List<ShareRedemptionEntity>();
		await foreach (var entity in client.QueryAsync<ShareRedemptionEntity>(e => e.PartitionKey == petId))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task<ShareRedemptionEntity?> GetRedemptionAsync(string petId, string deviceUserId)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		try
		{
			var response = await client.GetEntityAsync<ShareRedemptionEntity>(petId, deviceUserId);
			return response.Value;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public async Task<bool> RevokeRedemptionAsync(string petId, string deviceUserId)
	{
		var entity = await GetRedemptionAsync(petId, deviceUserId);
		if (entity is null) return false;

		entity.IsRevoked = true;
		var client = await GetTableClientAsync("ShareRedemptions");
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
		return true;
	}

	public async Task<bool> DeleteRedemptionAsync(string petId, string deviceUserId)
	{
		var entity = await GetRedemptionAsync(petId, deviceUserId);
		if (entity is null) return false;

		var client = await GetTableClientAsync("ShareRedemptions");
		try
		{
			await client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
			return true;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return false;
		}
	}

	public async Task<bool> DeletePetAsync(string petId)
	{
		var pet = await GetPetAsync(petId);
		if (pet is null)
			return false;

		await DeleteEntitiesByPartitionAsync("InsulinLogs", petId);
		await DeleteEntitiesByPartitionAsync("FeedingLogs", petId);
		await DeleteEntitiesByPartitionAsync("WeightLogs", petId);
		await DeleteEntitiesByPartitionAsync("VetInfos", petId);
		await DeleteEntitiesByPartitionAsync("Schedules", petId);
		await DeleteEntitiesByPartitionAsync("ShareCodes", petId);
		await DeleteEntitiesByPartitionAsync("ShareRedemptions", petId);

		var petClient = await GetTableClientAsync("Pets");
		await petClient.DeleteEntityAsync(pet.PartitionKey, pet.RowKey);
		return true;
	}

	private async Task DeleteEntitiesByPartitionAsync(string tableName, string partitionKey)
	{
		var client = await GetTableClientAsync(tableName);
		await foreach (var entity in client.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey))
		{
			await client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
		}
	}
}
