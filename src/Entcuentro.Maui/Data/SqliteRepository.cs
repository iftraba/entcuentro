using Entcuentro.Application.Interfaces;
using Entcuentro.Shared.Models;
using SQLite;

namespace Entcuentro.Maui.Data;

public class SqliteRepository<T> : IOfflineRepository<T> where T : SyncableEntity, new()
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null)
            return _db;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "myapp.db3");
        _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _db.CreateTableAsync<T>();
        return _db;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<T>().Where(e => !e.IsDeleted).ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var db = await GetDbAsync();
        return await db.Table<T>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task SaveAsync(T entity)
    {
        var db = await GetDbAsync();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsSynced = false;
        var existing = await db.Table<T>().Where(e => e.Id == entity.Id).FirstOrDefaultAsync();
        if (existing is null)
            await db.InsertAsync(entity);
        else
            await db.UpdateAsync(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var db = await GetDbAsync();
        var entity = await db.Table<T>().Where(e => e.Id == id).FirstOrDefaultAsync();
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.IsSynced = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.UpdateAsync(entity);
    }

    public async Task<IEnumerable<T>> GetUnsyncedAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<T>().Where(e => !e.IsSynced).ToListAsync();
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ids)
    {
        var db = await GetDbAsync();
        var idList = ids.ToList();
        var entities = await db.Table<T>().Where(e => idList.Contains(e.Id)).ToListAsync();
        foreach (var entity in entities)
            entity.IsSynced = true;
        await db.UpdateAllAsync(entities);
    }

    public async Task ApplyServerChangesAsync(IEnumerable<T> serverEntities)
    {
        var db = await GetDbAsync();
        foreach (var serverEntity in serverEntities)
        {
            serverEntity.IsSynced = true;
            var local = await db.Table<T>().Where(e => e.Id == serverEntity.Id).FirstOrDefaultAsync();
            if (local is null)
                await db.InsertAsync(serverEntity);
            else
                await db.UpdateAsync(serverEntity);
        }
    }
}
