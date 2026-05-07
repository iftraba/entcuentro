using Microsoft.EntityFrameworkCore;
using PlantillaDotNet.Application.Interfaces;
using PlantillaDotNet.Infrastructure.Data;
using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Infrastructure.Services;

public class ServerSyncService(AppDbContext db) : IServerSyncService
{
    public async Task<IEnumerable<T>> PullAsync<T>(DateTime since) where T : SyncableEntity
    {
        return await db.Set<T>()
            .Where(e => e.UpdatedAt > since)
            .ToListAsync();
    }

    public async Task PushAsync<T>(IEnumerable<T> clientChanges) where T : SyncableEntity
    {
        foreach (var clientEntity in clientChanges)
        {
            var serverEntity = await db.Set<T>().FindAsync(clientEntity.Id);

            if (serverEntity is null)
            {
                clientEntity.IsSynced = true;
                db.Set<T>().Add(clientEntity);
            }
            else if (clientEntity.UpdatedAt >= serverEntity.UpdatedAt)
            {
                // El cliente tiene datos más recientes: actualizar servidor
                db.Entry(serverEntity).CurrentValues.SetValues(clientEntity);
                serverEntity.IsSynced = true;
            }
            // Si serverEntity.UpdatedAt > clientEntity.UpdatedAt: servidor gana, no hacemos nada
        }

        await db.SaveChangesAsync();
    }
}
