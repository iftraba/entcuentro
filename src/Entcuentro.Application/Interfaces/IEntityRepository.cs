using Entcuentro.Shared.Models;

namespace Entcuentro.Application.Interfaces;

public interface IEntityRepository<T> where T : SyncableEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task SaveAsync(T entity);
    Task DeleteAsync(Guid id);
    void InvalidateCache();
}
