# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Idioma y estilo de trabajo

- Responder **siempre en castellano**.
- Antes de aplicar cualquier cambio, mostrar un resumen de lo que se va a hacer y pedir confirmación.
- Aplicar Clean Architecture, principios SOLID y nombres descriptivos en todo el código.

## Estructura de la solución

```
PruebaApp/
├── MyApp.slnx
└── src/
    ├── MyApp.Shared/           # DTOs, modelos, enums — compartido entre todos los proyectos
    ├── MyApp.UI/               # Razor Class Library — páginas y componentes Blazor compartidos
    ├── MyApp.Web/              # Blazor WebAssembly + PWA
    ├── MyApp.Maui/             # MAUI Blazor Hybrid (iOS y Android)
    ├── MyApp.Api/              # ASP.NET Core Web API
    ├── MyApp.Application/      # Interfaces de servicios — lógica de negocio
    └── MyApp.Infrastructure/   # EF Core, ASP.NET Identity, implementaciones
```

### Dependencias entre proyectos

```
MyApp.Web  ──→  MyApp.UI  ──→  MyApp.Shared
MyApp.Maui ──→  MyApp.UI
MyApp.Web  ──→  MyApp.Application
MyApp.Maui ──→  MyApp.Application
MyApp.Api  ──→  MyApp.Application  ──→  MyApp.Shared
MyApp.Api  ──→  MyApp.Infrastructure
MyApp.Infrastructure  ──→  MyApp.Application
MyApp.Infrastructure  ──→  MyApp.Shared
```

## Comandos habituales

```bash
# Compilar toda la solución (excepto MAUI, requiere Visual Studio)
dotnet build MyApp.slnx

# Ejecutar el API  →  https://localhost:7136
dotnet run --project src/MyApp.Api --launch-profile https

# Ejecutar la Web  →  https://localhost:7265
dotnet run --project src/MyApp.Web --launch-profile https

# Nueva migración EF
dotnet ef migrations add <Nombre> --project src/MyApp.Infrastructure --startup-project src/MyApp.Api

# Aplicar migraciones manualmente
dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api
```

MAUI requiere Visual Studio 2022+ con la carga de trabajo MAUI instalada.

## Base de datos

- **Motor:** SQL Server LocalDB (incluido con Visual Studio).
- **Cadena de conexión:** `src/MyApp.Api/appsettings.json` → `ConnectionStrings:DefaultConnection`
- **ORM:** Entity Framework Core 10, Code-First. Migraciones en `MyApp.Infrastructure/Data/Migrations/`.
- **DbContext:** `AppDbContext` en `src/MyApp.Infrastructure/Data/AppDbContext.cs`
- Las migraciones pendientes **se aplican automáticamente** al arrancar el API (`db.Database.Migrate()` en `Program.cs`).

### Usuarios de prueba (sembrados automáticamente)

| Email | Contraseña | Rol |
|---|---|---|
| admin@myapp.com | Admin1234! | Administrador |
| juan@myapp.com | Usuario1234! | Usuario |
| maria@myapp.com | Usuario1234! | Usuario |

## Autenticación y autorización

- **Servidor:** ASP.NET Core Identity + JWT Bearer.
- **JWT:** clave en `appsettings.json → Jwt:Key`. Cambiarla antes de producción (≥32 caracteres).
- **Endpoints:** `POST /api/auth/login` y `POST /api/auth/register`.
- **Roles:** `RolNombre.Administrador` y `RolNombre.Usuario` (constantes en `MyApp.Shared/Enums/RolNombre.cs`).

### Patrón ITokenManager

Ambos proveedores de auth implementan `ITokenManager` (en `MyApp.UI/Auth/`):

```
ITokenManager
  NotifyAuthenticatedAsync(token)   ← guarda token + notifica estado
  NotifyLoggedOutAsync()            ← elimina token + notifica estado
  GetRememberedEmailAsync()         ← recuerda usuario entre sesiones
  SaveRememberedEmailAsync(email)
  RemoveRememberedEmailAsync()
```

- **Web:** `JwtAuthStateProvider` → token en `localStorage`, email recordado en `localStorage`.
- **MAUI:** `MauiJwtAuthStateProvider` → token en `SecureStorage`, email recordado en `Preferences`.

Registro DI (instancia compartida para ambas interfaces):
```csharp
services.AddScoped<JwtAuthStateProvider>();  // o AddSingleton para MAUI
services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
services.AddScoped<ITokenManager>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
```

## Pantalla de login (MyApp.UI)

- Página: `MyApp.UI/Pages/Login.razor` → ruta `/login`, usa `@layout LoginLayout`.
- Layout vacío sin nav: `MyApp.UI/Layouts/LoginLayout.razor` (fondo degradado + footer con versión).
- Redirección automática: `MyApp.UI/Components/RedirectToLogin.razor`.
- Los routers (`App.razor` en Web y `Routes.razor` en MAUI) usan `AdditionalAssemblies` para incluir `MyApp.UI` y `RedirectToLogin` en el bloque `<NotAuthorized>`.
- Las páginas protegidas llevan `@attribute [Authorize]`.

## Soporte offline (infraestructura implementada, pendiente de conectar a entidades reales)

### MyApp.Shared
- `SyncableEntity` — clase base con `Id (Guid)`, `UpdatedAt`, `IsSynced`, `IsDeleted`.
- `SyncDtos` — `SyncPushRequest<T>` y `SyncPullResponse<T>`.

### MyApp.Application
- `IOfflineRepository<T>` — CRUD local + `GetUnsyncedAsync`, `MarkAsSyncedAsync`, `ApplyServerChangesAsync`.
- `ISyncService` — `SyncAsync()`, `IsSyncing`, evento `SyncCompleted`.
- `IServerSyncService` — `PullAsync<T>(since)` y `PushAsync<T>(changes)`.

### MyApp.Infrastructure
- `ServerSyncService` — implementa pull/push con EF Core (server wins).

### MyApp.Api
- `SyncController` — `GET /api/sync/pull?since=` y `POST /api/sync/push`.

### MyApp.Web
- `IndexedDbRepository<T>` — implementa `IOfflineRepository<T>` via JS interop.
- `WebSyncService` — detecta `window.online/offline`, llama `SyncAsync()` al reconectar.
- `wwwroot/js/indexedDb.js` — wrapper JS de IndexedDB.

### MyApp.Maui
- `SqliteRepository<T>` — implementa `IOfflineRepository<T>` con `sqlite-net-pcl`.
- `MauiSyncService` — usa `IConnectivity` para detectar red, llama `SyncAsync()` al reconectar.

## Añadir una nueva entidad (flujo completo)

1. **DTO/Modelo** → `MyApp.Shared` (heredar de `SyncableEntity` si necesita offline)
2. **Interfaz de servicio** → `MyApp.Application/Interfaces/`
3. **Implementación** → `MyApp.Infrastructure/Services/`
4. **Registro DI** → `MyApp.Infrastructure/DependencyInjection.cs`
5. **Migración EF** → `dotnet ef migrations add ...`
6. **Controlador** → `MyApp.Api/Controllers/`
7. **Componente Blazor compartido** → `MyApp.UI/`

## CORS

`src/MyApp.Api/appsettings.json → AllowedOrigins` debe incluir la URL de la Web:
- Desarrollo: `https://localhost:7265`

## MAUI — emulador Android

El emulador Android accede al API en `https://10.0.2.2:7136` (no `localhost`). Configurado en `src/MyApp.Maui/MauiProgram.cs`.
