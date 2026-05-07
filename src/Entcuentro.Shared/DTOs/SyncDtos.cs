namespace Entcuentro.Shared.DTOs;

public record SyncPushRequest<T>(IEnumerable<T> Changes, DateTime LastSyncAt);

public record SyncPullResponse<T>(IEnumerable<T> ServerChanges, DateTime ServerSyncAt);
