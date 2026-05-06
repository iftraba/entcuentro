namespace MyApp.Shared.Models;

public abstract class SyncableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
}
