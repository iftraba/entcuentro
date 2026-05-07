Eres un asistente especializado en este proyecto Entcuentro. Tu tarea es conectar una entidad existente a la infraestructura de sincronización offline ya implementada.

## Contexto del proyecto

La infraestructura offline ya existe y está completamente implementada pero no conectada a ninguna entidad real:

- `ISyncService` / `WebSyncService` / `MauiSyncService` — detectan reconexión y ejecutan sync
- `IOfflineRepository<T>` / `IndexedDbRepository<T>` / `SqliteRepository<T>` — BD local
- `SyncController` — endpoints `GET /api/sync/pull?since=` y `POST /api/sync/push`
- `IServerSyncService` / `ServerSyncService` — pull/push en servidor con EF Core
- `CachedRepository<T>` — ya llama a `InvalidateCache()` en save/delete, pero no tras sync

El método `SyncAsync()` en `WebSyncService` y `MauiSyncService` tiene un comentario de ejemplo pero no llama a nada real todavía.

## Antes de empezar

Si el usuario no ha indicado la entidad, pregúntale:
1. **Nombre** de la entidad a conectar (debe heredar de `SyncableEntity`)
2. **Ruta del endpoint de sync** — por defecto usa la convención `typeof(T).Name.ToLower()` (ej. `Producto` → `"producto"`)

Muestra un resumen de los archivos a modificar y pide confirmación.

---

## Archivos a modificar (en este orden)

### 1. `src/Entcuentro.Api/Controllers/SyncController.cs`

El controlador ya tiene los endpoints genéricos. Si la entidad usa `IServerSyncService<T>`, no hay nada que añadir. Si el controlador es tipado por entidad, añadir las rutas específicas:

```csharp
// Dentro de SyncController, añadir endpoints específicos si es necesario
[HttpGet("{entidad}/pull")]
public async Task<IActionResult> Pull{Entidad}([FromQuery] DateTime? since, 
    [FromServices] IServerSyncService serverSync)
{
    var changes = await serverSync.PullAsync<{Entidad}>(since ?? DateTime.MinValue);
    return Ok(new SyncPullResponse<{Entidad}>(changes, DateTime.UtcNow));
}

[HttpPost("{entidad}/push")]
public async Task<IActionResult> Push{Entidad}(
    [FromBody] SyncPushRequest<{Entidad}> request,
    [FromServices] IServerSyncService serverSync)
{
    await serverSync.PushAsync(request.Changes, request.LastSyncAt);
    return Ok();
}
```

### 2. `src/Entcuentro.Web/Services/WebSyncService.cs`

Añadir la llamada al método `SyncEntityAsync` ya existente dentro de `SyncAsync()`:

```csharp
public async Task SyncAsync()
{
    if (_isSyncing) return;
    _isSyncing = true;
    try
    {
        // Añadir una línea por cada entidad conectada a offline:
        await SyncEntityAsync<{Entidad}>(
            serviceProvider.GetRequiredService<IOfflineRepository<{Entidad}>>(),
            "{entidad_ruta}");

        // Invalidar la caché de IEntityRepository para que los componentes
        // recarguen datos frescos tras la sincronización:
        serviceProvider.GetRequiredService<IEntityRepository<{Entidad}>>()
            .InvalidateCache();

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
```

**IMPORTANTE:** Para que `WebSyncService` pueda resolver `IOfflineRepository<T>` e `IEntityRepository<T>` en `SyncAsync`, necesita recibir `IServiceProvider` en el constructor. Modificar el constructor:

```csharp
public class WebSyncService(HttpClient httpClient, IJSRuntime js, IServiceProvider serviceProvider) 
    : ISyncService, IAsyncDisposable
```

Y registrar en `Program.cs` como `AddScoped` (ya lo está).

### 3. `src/Entcuentro.Maui/Services/MauiSyncService.cs`

Mismo patrón que `WebSyncService`. Añadir dentro de `SyncAsync()`:

```csharp
await SyncEntityAsync<{Entidad}>(
    serviceProvider.GetRequiredService<IOfflineRepository<{Entidad}>>(),
    "{entidad_ruta}");

serviceProvider.GetRequiredService<IEntityRepository<{Entidad}>>()
    .InvalidateCache();
```

Si `MauiSyncService` no tiene `IServiceProvider`, añadirlo al constructor:
```csharp
public class MauiSyncService(IConnectivity connectivity, HttpClient httpClient, 
    IJSRuntime js, IServiceProvider serviceProvider) : ISyncService
```

### 4. Verificar que `SyncableEntity` está correctamente configurada en Infrastructure

En `AppDbContext.cs`, la entidad ya debe tener su `DbSet<T>`. Si no es así, añadirlo (ver `/nueva-entidad`).

En `ServerSyncService.cs`, el método genérico `PullAsync<T>` y `PushAsync<T>` deben poder acceder al `DbSet<T>` de la entidad. Si el `ServerSyncService` usa reflexión o un `Set<T>()` de EF Core, verificar que funciona con la nueva entidad. EF Core `db.Set<T>()` funciona automáticamente con cualquier entidad registrada.

### 5. Verificar compilación

```bash
dotnet build Entcuentro.slnx
```

---

## Flujo completo tras conectar

```
Dispositivo pierde red → trabaja con IEntityRepository<{Entidad}> (caché + BD local)
         │
         ▼
Recupera red → WebSyncService/MauiSyncService detecta evento "online"
         │
         ▼
SyncAsync()
  ├─ SyncEntityAsync<{Entidad}>()
  │    ├─ POST /api/sync/{entidad}/push  → cambios locales al servidor
  │    └─ GET  /api/sync/{entidad}/pull  → novedades del servidor a BD local
  └─ IEntityRepository<{Entidad}>.InvalidateCache()
         │
         ▼
Próxima llamada a Repo.GetAllAsync() → recarga desde BD local (ya actualizada)
```

---

## Notas importantes

- El servidor siempre gana en conflictos (`ApplyServerChangesAsync` sobreescribe local con datos del servidor si `UpdatedAt` del servidor es más reciente).
- `InvalidateCache()` solo borra el diccionario en memoria; los datos de la BD local (SQLite/IndexedDB) no se borran, se actualizan con los del servidor.
- Si hay varias entidades offline, añadir una llamada `SyncEntityAsync` por cada una en orden.
- Tras este cambio, el `IsSyncing` de `ISyncService` puede usarse en la UI para mostrar un indicador de sincronización.
