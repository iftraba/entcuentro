using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Application.Interfaces;

public interface IServerSyncService
{
    Task<IEnumerable<T>> PullAsync<T>(DateTime since) where T : SyncableEntity;
    Task PushAsync<T>(IEnumerable<T> clientChanges) where T : SyncableEntity;
}
