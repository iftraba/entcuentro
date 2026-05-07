namespace PlantillaDotNet.Application.Interfaces;

public class SyncCompletedEventArgs(bool success, string? errorMessage = null) : EventArgs
{
    public bool Success { get; } = success;
    public string? ErrorMessage { get; } = errorMessage;
}

public interface ISyncService
{
    bool IsSyncing { get; }
    event EventHandler<SyncCompletedEventArgs> SyncCompleted;
    Task SyncAsync();
}
