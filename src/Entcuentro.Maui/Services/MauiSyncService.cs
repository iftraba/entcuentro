using System.Net.Http.Json;
using Entcuentro.Application.Interfaces;
using Entcuentro.Shared.DTOs;
using Entcuentro.Shared.Models;

namespace Entcuentro.Maui.Services;

public class MauiSyncService(HttpClient httpClient) : ISyncService, IDisposable
{
    private const string LastSyncKey = "lastSyncAt";
    private bool _isSyncing;

    public bool IsSyncing => _isSyncing;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public void StartMonitoring()
    {
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            await SyncAsync();
    }

    public async Task SyncAsync()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            // Cada tipo de entidad se sincronizará aquí cuando existan entidades reales.
            // Ejemplo (descomentar cuando exista la entidad):
            // await SyncEntityAsync<TodoItem>(offlineRepo);

            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(true));
        }
        catch (Exception ex)
        {
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(false, ex.Message));
        }
        finally
        {
            _isSyncing = false;
        }
    }

    // Método genérico de sync para cualquier entidad. Llamar desde SyncAsync() por cada tipo.
    protected async Task SyncEntityAsync<T>(IOfflineRepository<T> repository, string entityRoute)
        where T : SyncableEntity
    {
        var lastSyncAt = Preferences.Default.Get($"{LastSyncKey}_{typeof(T).Name}", DateTime.MinValue.ToString("O"));
        var lastSync = DateTime.Parse(lastSyncAt);

        // Push: enviar cambios locales no sincronizados
        var unsynced = (await repository.GetUnsyncedAsync()).ToList();
        if (unsynced.Count > 0)
        {
            var pushRequest = new SyncPushRequest<T>(unsynced, lastSync);
            await httpClient.PostAsJsonAsync($"api/sync/{entityRoute}/push", pushRequest);
            await repository.MarkAsSyncedAsync(unsynced.Select(e => e.Id));
        }

        // Pull: obtener cambios del servidor desde la última sincronización
        var pullUrl = $"api/sync/{entityRoute}/pull?since={Uri.EscapeDataString(lastSync.ToString("O"))}";
        var response = await httpClient.GetFromJsonAsync<SyncPullResponse<T>>(pullUrl);
        if (response?.ServerChanges.Any() == true)
            await repository.ApplyServerChangesAsync(response.ServerChanges);

        Preferences.Default.Set($"{LastSyncKey}_{typeof(T).Name}", DateTime.UtcNow.ToString("O"));
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
