using Azure.Data.Tables;
using PetInsulinTracker.Api.Models;

namespace PetInsulinTracker.Api.Services;

public class TableStorageService
{
	private readonly TableServiceClient _serviceClient;

	public TableStorageService()
	{
		var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString")
			?? "UseDevelopmentStorage=true";
		_serviceClient = new TableServiceClient(connectionString);
	}

	private async Task<TableClient> GetTableClientAsync(string tableName)
	{
		var client = _serviceClient.GetTableClient(tableName);
		await client.CreateIfNotExistsAsync();
		return client;
	}

	// Pets
	public async Task UpsertPetAsync(string shareCode, PetEntity entity)
	{
		var client = await GetTableClientAsync("Pets");
		entity.PartitionKey = shareCode;
		entity.RowKey = entity.RowKey.Length > 0 ? entity.RowKey : Guid.NewGuid().ToString();
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	public async Task<PetEntity?> GetPetAsync(string shareCode, string petId)
	{
		var client = await GetTableClientAsync("Pets");
		try
		{
			var response = await client.GetEntityAsync<PetEntity>(shareCode, petId);
			return response.Value;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public async Task<List<PetEntity>> GetPetsByShareCodeAsync(string shareCode)
	{
		var client = await GetTableClientAsync("Pets");
		var results = new List<PetEntity>();
		await foreach (var entity in client.QueryAsync<PetEntity>(e => e.PartitionKey == shareCode))
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
		var results = new List<T>();
		await foreach (var entity in client.QueryAsync<T>(
			e => e.PartitionKey == partitionKey && e.Timestamp >= since))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task<List<PetEntity>> GetPetsModifiedSinceAsync(string shareCode, DateTimeOffset since)
	{
		var client = await GetTableClientAsync("Pets");
		var results = new List<PetEntity>();
		await foreach (var entity in client.QueryAsync<PetEntity>(
			e => e.PartitionKey == shareCode && e.Timestamp >= since))
		{
			results.Add(entity);
		}
		return results;
	}

	// Share codes
	public async Task<ShareCodeEntity?> GetShareCodeAsync(string code)
	{
		var client = await GetTableClientAsync("ShareCodes");
		try
		{
			var response = await client.GetEntityAsync<ShareCodeEntity>("ShareCodes", code);
			return response.Value;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public async Task CreateShareCodeAsync(string code, string petId, string accessLevel = "full")
	{
		var client = await GetTableClientAsync("ShareCodes");
		var entity = new ShareCodeEntity
		{
			PartitionKey = "ShareCodes",
			RowKey = code,
			PetId = petId,
			AccessLevel = accessLevel
		};
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	// Share redemptions
	public async Task CreateRedemptionAsync(string shareCode, string deviceUserId, string displayName, string accessLevel)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		var entity = new ShareRedemptionEntity
		{
			PartitionKey = shareCode,
			RowKey = deviceUserId,
			DisplayName = displayName,
			AccessLevel = accessLevel
		};
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
	}

	public async Task<List<ShareRedemptionEntity>> GetRedemptionsAsync(string shareCode)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		var results = new List<ShareRedemptionEntity>();
		await foreach (var entity in client.QueryAsync<ShareRedemptionEntity>(e => e.PartitionKey == shareCode))
		{
			results.Add(entity);
		}
		return results;
	}

	public async Task<ShareRedemptionEntity?> GetRedemptionAsync(string shareCode, string deviceUserId)
	{
		var client = await GetTableClientAsync("ShareRedemptions");
		try
		{
			var response = await client.GetEntityAsync<ShareRedemptionEntity>(shareCode, deviceUserId);
			return response.Value;
		}
		catch (Azure.RequestFailedException ex) when (ex.Status == 404)
		{
			return null;
		}
	}

	public async Task<bool> RevokeRedemptionAsync(string shareCode, string deviceUserId)
	{
		var entity = await GetRedemptionAsync(shareCode, deviceUserId);
		if (entity is null) return false;

		entity.IsRevoked = true;
		var client = await GetTableClientAsync("ShareRedemptions");
		await client.UpsertEntityAsync(entity, TableUpdateMode.Replace);
		return true;
	}
}
