using System.Net.Http.Json;
using PlantillaDotNet.Application.Interfaces;
using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Application.Repositories;

/// <summary>
/// Repositorio genérico con caché en 3 capas:
///   1. Diccionario en memoria (por tipo, estático → persiste toda la sesión)
///   2. BD local (SQLite en MAUI, IndexedDB en Web)
///   3. API REST — convención: GET /api/{tipo_en_plural}/{id}
///
/// Al encontrar datos en capa N los propaga hacia arriba (capas más rápidas).
/// Llamar a InvalidateCache() tras una sincronización para forzar refresco.
/// </summary>
public class CachedRepository<T>(IOfflineRepository<T> localRepo, HttpClient httpClient)
    : IEntityRepository<T>
    where T : SyncableEntity
{
    // Estático por tipo cerrado: CachedRepository<Product>._cache ≠ CachedRepository<Order>._cache
    private static readonly Dictionary<Guid, T> _cache = [];
    private static bool _allFetched;

    // Convención REST: Product → "products", Order → "orders"
    private static string Route => typeof(T).Name.ToLower() + "s";

    public async Task<T?> GetByIdAsync(Guid id)
    {
        // 1. Caché en memoria
        if (_cache.TryGetValue(id, out var hit)) return hit;

        // 2. BD local
        var local = await localRepo.GetByIdAsync(id);
        if (local is not null)
        {
            _cache[id] = local;
            return local;
        }

        // 3. API → guarda en BD local y en caché
        try
        {
            var remote = await httpClient.GetFromJsonAsync<T>($"api/{Route}/{id}");
            if (remote is not null)
            {
                remote.IsSynced = true;
                await localRepo.SaveAsync(remote);
                _cache[id] = remote;
            }
            return remote;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // 1. Caché en memoria (si ya se cargó la colección completa)
        if (_allFetched)
            return _cache.Values.Where(e => !e.IsDeleted).ToList();

        // 2. BD local
        var locals = (await localRepo.GetAllAsync()).ToList();
        if (locals.Count > 0)
        {
            foreach (var e in locals) _cache[e.Id] = e;
            _allFetched = true;
            return locals;
        }

        // 3. API → guarda todo en BD local y en caché
        try
        {
            var remotes = await httpClient.GetFromJsonAsync<List<T>>($"api/{Route}") ?? [];
            foreach (var e in remotes)
            {
                e.IsSynced = true;
                await localRepo.SaveAsync(e);
                _cache[e.Id] = e;
            }
            _allFetched = true;
            return remotes.Where(e => !e.IsDeleted).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveAsync(T entity)
    {
        await localRepo.SaveAsync(entity);
        _cache[entity.Id] = entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        await localRepo.DeleteAsync(id);
        _cache.Remove(id);
    }

    public void InvalidateCache()
    {
        _cache.Clear();
        _allFetched = false;
    }
}
