using MyApp.Shared.Models;

namespace MyApp.Application.Interfaces;

public interface IOfflineRepository<T> where T : SyncableEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task SaveAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<T>> GetUnsyncedAsync();
    Task MarkAsSyncedAsync(IEnumerable<Guid> ids);
    Task ApplyServerChangesAsync(IEnumerable<T> serverEntities);
}
