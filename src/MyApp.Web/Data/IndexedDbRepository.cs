using System.Text.Json;
using Microsoft.JSInterop;
using MyApp.Application.Interfaces;
using MyApp.Shared.Models;

namespace MyApp.Web.Data;

public class IndexedDbRepository<T>(IJSRuntime js) : IOfflineRepository<T>
    where T : SyncableEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _storeName = typeof(T).Name;

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var result = await js.InvokeAsync<JsonElement>("MyAppIndexedDb.getAll", _storeName);
        return Deserialize(result).Where(e => !e.IsDeleted);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var result = await js.InvokeAsync<JsonElement?>("MyAppIndexedDb.getById", _storeName, id.ToString());
        if (result is null) return null;
        return JsonSerializer.Deserialize<T>(result.Value.GetRawText(), JsonOptions);
    }

    public async Task SaveAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsSynced = false;
        var json = Serialize(entity);
        await js.InvokeVoidAsync("MyAppIndexedDb.put", _storeName, json);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.IsSynced = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await SaveAsync(entity);
    }

    public async Task<IEnumerable<T>> GetUnsyncedAsync()
    {
        var result = await js.InvokeAsync<JsonElement>("MyAppIndexedDb.getUnsynced", _storeName);
        return Deserialize(result);
    }

    public async Task MarkAsSyncedAsync(IEnumerable<Guid> ids)
    {
        var idSet = ids.ToHashSet();
        var entities = (await GetAllAsync()).Where(e => idSet.Contains(e.Id)).ToList();
        foreach (var entity in entities)
            entity.IsSynced = true;
        var jsonArray = entities.Select(Serialize).ToArray();
        await js.InvokeVoidAsync("MyAppIndexedDb.putBatch", _storeName, jsonArray);
    }

    public async Task ApplyServerChangesAsync(IEnumerable<T> serverEntities)
    {
        var entities = serverEntities.Select(e => { e.IsSynced = true; return e; }).ToList();
        var jsonArray = entities.Select(Serialize).ToArray();
        await js.InvokeVoidAsync("MyAppIndexedDb.putBatch", _storeName, jsonArray);
    }

    private static object Serialize(T entity) =>
        JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(entity, JsonOptions), JsonOptions)!;

    private static IEnumerable<T> Deserialize(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array) return [];
        return element.EnumerateArray()
            .Select(e => JsonSerializer.Deserialize<T>(e.GetRawText(), JsonOptions))
            .Where(e => e is not null)
            .Cast<T>();
    }
}
