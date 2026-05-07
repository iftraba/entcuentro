using System.Net.Http.Json;
using Microsoft.JSInterop;
using Entcuentro.Application.Interfaces;
using Entcuentro.Shared.DTOs;
using Entcuentro.Shared.Models;

namespace Entcuentro.Web.Services;

public class WebSyncService(HttpClient httpClient, IJSRuntime js) : ISyncService, IAsyncDisposable
{
    private const string LastSyncKey = "lastSyncAt";
    private bool _isSyncing;
    private DotNetObjectReference<WebSyncService>? _objRef;
    private IJSObjectReference? _subscription;

    public bool IsSyncing => _isSyncing;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public async Task StartMonitoringAsync()
    {
        _objRef = DotNetObjectReference.Create(this);
        _subscription = await js.InvokeAsync<IJSObjectReference>(
            "EntcuentroIndexedDb.subscribeOnline", _objRef);
    }

    [JSInvokable]
    public async Task OnOnline() => await SyncAsync();

    public async Task SyncAsync()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            // Cada tipo de entidad se sincroniza aquí cuando existan entidades reales.
            // Ejemplo:
            // await SyncEntityAsync<TodoItem>(todoRepository, "todo");

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

    protected async Task SyncEntityAsync<T>(IOfflineRepository<T> repository, string entityRoute)
        where T : SyncableEntity
    {
        var lastSyncRaw = await js.InvokeAsync<string?>("localStorage.getItem", $"{LastSyncKey}_{typeof(T).Name}");
        var lastSync = lastSyncRaw is not null ? DateTime.Parse(lastSyncRaw) : DateTime.MinValue;

        var unsynced = (await repository.GetUnsyncedAsync()).ToList();
        if (unsynced.Count > 0)
        {
            var pushRequest = new SyncPushRequest<T>(unsynced, lastSync);
            await httpClient.PostAsJsonAsync($"api/sync/{entityRoute}/push", pushRequest);
            await repository.MarkAsSyncedAsync(unsynced.Select(e => e.Id));
        }

        var pullUrl = $"api/sync/{entityRoute}/pull?since={Uri.EscapeDataString(lastSync.ToString("O"))}";
        var response = await httpClient.GetFromJsonAsync<SyncPullResponse<T>>(pullUrl);
        if (response?.ServerChanges.Any() == true)
            await repository.ApplyServerChangesAsync(response.ServerChanges);

        await js.InvokeVoidAsync("localStorage.setItem",
            $"{LastSyncKey}_{typeof(T).Name}", DateTime.UtcNow.ToString("O"));
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscription is not null)
            await _subscription.InvokeVoidAsync("dispose");
        _objRef?.Dispose();
    }
}
