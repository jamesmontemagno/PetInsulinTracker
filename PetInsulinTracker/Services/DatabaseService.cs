using PetInsulinTracker.Models;
using SQLite;

namespace PetInsulinTracker.Services;

public class DatabaseService : IDatabaseService
{
	private SQLiteAsyncConnection? _db;

	private async Task<SQLiteAsyncConnection> GetConnectionAsync()
	{
		if (_db is not null)
			return _db;

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "petinsulintracker.db3");
		_db = new SQLiteAsyncConnection(dbPath);

		await _db.CreateTableAsync<Pet>();
		await _db.CreateTableAsync<InsulinLog>();
		await _db.CreateTableAsync<FeedingLog>();
		await _db.CreateTableAsync<WeightLog>();
		await _db.CreateTableAsync<VetInfo>();
		await _db.CreateTableAsync<Schedule>();

		return _db;
	}

	// Pets

	public async Task<List<Pet>> GetPetsAsync()
	{
		var db = await GetConnectionAsync();
		return await db.Table<Pet>().OrderBy(p => p.Name).ToListAsync();
	}

	public async Task<Pet?> GetPetAsync(string id)
	{
		var db = await GetConnectionAsync();
		return await db.Table<Pet>().FirstOrDefaultAsync(p => p.Id == id);
	}

	public async Task<int> SavePetAsync(Pet pet)
	{
		pet.LastModified = DateTimeOffset.UtcNow;
		pet.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<Pet>().FirstOrDefaultAsync(p => p.Id == pet.Id);
		return existing is null
			? await db.InsertAsync(pet)
			: await db.UpdateAsync(pet);
	}

	public async Task<int> DeletePetAsync(Pet pet)
	{
		var db = await GetConnectionAsync();
		return await db.DeleteAsync(pet);
	}

	// Insulin Logs

	public async Task<List<InsulinLog>> GetInsulinLogsAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<InsulinLog>()
			.Where(l => l.PetId == petId)
			.OrderByDescending(l => l.AdministeredAt)
			.ToListAsync();
	}

	public async Task<InsulinLog?> GetLatestInsulinLogAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<InsulinLog>()
			.Where(l => l.PetId == petId)
			.OrderByDescending(l => l.AdministeredAt)
			.FirstOrDefaultAsync();
	}

	public async Task<int> SaveInsulinLogAsync(InsulinLog log)
	{
		log.LastModified = DateTimeOffset.UtcNow;
		log.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<InsulinLog>().FirstOrDefaultAsync(l => l.Id == log.Id);
		return existing is null
			? await db.InsertAsync(log)
			: await db.UpdateAsync(log);
	}

	public async Task<int> DeleteInsulinLogAsync(InsulinLog log)
	{
		var db = await GetConnectionAsync();
		return await db.DeleteAsync(log);
	}

	// Feeding Logs

	public async Task<List<FeedingLog>> GetFeedingLogsAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<FeedingLog>()
			.Where(l => l.PetId == petId)
			.OrderByDescending(l => l.FedAt)
			.ToListAsync();
	}

	public async Task<int> SaveFeedingLogAsync(FeedingLog log)
	{
		log.LastModified = DateTimeOffset.UtcNow;
		log.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<FeedingLog>().FirstOrDefaultAsync(l => l.Id == log.Id);
		return existing is null
			? await db.InsertAsync(log)
			: await db.UpdateAsync(log);
	}

	public async Task<int> DeleteFeedingLogAsync(FeedingLog log)
	{
		var db = await GetConnectionAsync();
		return await db.DeleteAsync(log);
	}

	// Weight Logs

	public async Task<List<WeightLog>> GetWeightLogsAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<WeightLog>()
			.Where(l => l.PetId == petId)
			.OrderByDescending(l => l.RecordedAt)
			.ToListAsync();
	}

	public async Task<WeightLog?> GetLatestWeightLogAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<WeightLog>()
			.Where(l => l.PetId == petId)
			.OrderByDescending(l => l.RecordedAt)
			.FirstOrDefaultAsync();
	}

	public async Task<int> SaveWeightLogAsync(WeightLog log)
	{
		log.LastModified = DateTimeOffset.UtcNow;
		log.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<WeightLog>().FirstOrDefaultAsync(l => l.Id == log.Id);
		return existing is null
			? await db.InsertAsync(log)
			: await db.UpdateAsync(log);
	}

	public async Task<int> DeleteWeightLogAsync(WeightLog log)
	{
		var db = await GetConnectionAsync();
		return await db.DeleteAsync(log);
	}

	// Vet Info

	public async Task<VetInfo?> GetVetInfoAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<VetInfo>().FirstOrDefaultAsync(v => v.PetId == petId);
	}

	public async Task<int> SaveVetInfoAsync(VetInfo info)
	{
		info.LastModified = DateTimeOffset.UtcNow;
		info.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<VetInfo>().FirstOrDefaultAsync(v => v.Id == info.Id);
		return existing is null
			? await db.InsertAsync(info)
			: await db.UpdateAsync(info);
	}

	// Schedules

	public async Task<List<Schedule>> GetSchedulesAsync(string petId)
	{
		var db = await GetConnectionAsync();
		return await db.Table<Schedule>()
			.Where(s => s.PetId == petId)
			.OrderBy(s => s.TimeTicks)
			.ToListAsync();
	}

	public async Task<List<Schedule>> GetAllEnabledSchedulesAsync()
	{
		var db = await GetConnectionAsync();
		return await db.Table<Schedule>()
			.Where(s => s.IsEnabled)
			.ToListAsync();
	}

	public async Task<int> SaveScheduleAsync(Schedule schedule)
	{
		schedule.LastModified = DateTimeOffset.UtcNow;
		schedule.IsSynced = false;
		var db = await GetConnectionAsync();
		var existing = await db.Table<Schedule>().FirstOrDefaultAsync(s => s.Id == schedule.Id);
		return existing is null
			? await db.InsertAsync(schedule)
			: await db.UpdateAsync(schedule);
	}

	public async Task<int> DeleteScheduleAsync(Schedule schedule)
	{
		var db = await GetConnectionAsync();
		return await db.DeleteAsync(schedule);
	}

	// Sync support

	public async Task<List<T>> GetUnsyncedAsync<T>() where T : new()
	{
		var db = await GetConnectionAsync();
		return await db.QueryAsync<T>(
			$"SELECT * FROM [{typeof(T).Name}] WHERE [IsSynced] = 0");
	}

	public async Task MarkSyncedAsync<T>(string id) where T : new()
	{
		var db = await GetConnectionAsync();
		await db.ExecuteAsync(
			$"UPDATE [{typeof(T).Name}] SET [IsSynced] = 1 WHERE [Id] = ?", id);
	}
}
