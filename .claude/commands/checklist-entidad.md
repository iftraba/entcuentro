Eres un asistente especializado en este proyecto Entcuentro. Tu tarea es auditar una entidad existente y verificar que tiene todos los componentes necesarios correctamente implementados.

## Antes de empezar

Si el usuario no ha indicado la entidad, pregúntale el nombre (ej. `Producto`).

No modifiques nada sin pedir confirmación. Este comando es solo de lectura y diagnóstico.

---

## Checklist — leer y verificar en este orden

Para cada punto, marca ✅ (correcto), ⚠️ (existe pero tiene problemas), o ❌ (falta).

### 1. Modelo (`Entcuentro.Shared/Models/{Entidad}.cs`)
- [ ] El archivo existe
- [ ] Hereda de `SyncableEntity`
- [ ] Tiene `Id`, `UpdatedAt`, `IsSynced`, `IsDeleted` (heredados, no repetidos)
- [ ] Los campos tienen `DataAnnotations` apropiados (`[Required]`, `[MaxLength]`, etc.)
- [ ] Strings inicializados a `string.Empty`

### 2. DTOs (`Entcuentro.Shared/DTOs/{Entidad}Dtos.cs`)
- [ ] El archivo existe
- [ ] Tiene `Create{Entidad}Request` con validaciones
- [ ] Tiene `Update{Entidad}Request` con `Guid Id` + campos actualizables
- [ ] Tiene `{Entidad}Response` con los campos que devuelve el API (nunca la entidad directamente)

### 3. Interfaz de servicio (`Entcuentro.Application/Interfaces/I{Entidad}Service.cs`)
- [ ] El archivo existe
- [ ] Tiene los métodos: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- [ ] Devuelve `{Entidad}Response` (DTO), no la entidad de dominio

### 4. Implementación (`Entcuentro.Infrastructure/Services/{Entidad}Service.cs`)
- [ ] El archivo existe
- [ ] Implementa `I{Entidad}Service`
- [ ] Los borrados son soft-delete (`IsDeleted = true`, no `db.Remove()`)
- [ ] Actualiza `UpdatedAt = DateTime.UtcNow` en Create y Update
- [ ] Marca `IsSynced = false` en Update (indica cambio local pendiente de sync)
- [ ] Devuelve `{Entidad}Response` a través de un método `ToResponse()` privado

### 5. DbContext (`Entcuentro.Infrastructure/Data/AppDbContext.cs`)
- [ ] Tiene `public DbSet<{Entidad}> {Entidades} { get; set; }`

### 6. Registro DI (`Entcuentro.Infrastructure/DependencyInjection.cs`)
- [ ] Tiene `services.AddScoped<I{Entidad}Service, {Entidad}Service>()`

### 7. Migración EF
- [ ] Existe un archivo de migración en `Entcuentro.Infrastructure/Migrations/` que crea la tabla `{Entidades}`
- [ ] La migración está aplicada (verificar con `dotnet ef migrations list`)

### 8. Controlador API (`Entcuentro.Api/Controllers/{Entidades}Controller.cs`)
- [ ] El archivo existe
- [ ] Tiene `[ApiController]` y `[Authorize]`
- [ ] La ruta base es `[Route("api/{entidades}")]` — CRÍTICO: debe coincidir con la convención de `IEntityRepository<T>` (`typeof(T).Name.ToLower() + "s"`)
- [ ] Tiene `GET /api/{entidades}` → devuelve lista (necesario para `Repo.GetAllAsync()`)
- [ ] Tiene `GET /api/{entidades}/{id}` → devuelve uno (necesario para `Repo.GetByIdAsync()`)
- [ ] Tiene `POST`, `PUT`, `DELETE`
- [ ] Devuelve DTOs, nunca entidades de dominio

### 9. Caché (`IEntityRepository<{Entidad}>`)
- [ ] `IEntityRepository<>` está registrado como `AddScoped` en Web y `AddSingleton` en MAUI (registro genérico, no específico por entidad — ya configurado globalmente)
- [ ] Los componentes Blazor usan `@inject IEntityRepository<{Entidad}> Repo` (no `I{Entidad}Service` directamente)

### 10. Soporte offline (opcional — solo si la entidad necesita funcionar sin red)
- [ ] `WebSyncService.SyncAsync()` tiene `SyncEntityAsync<{Entidad}>(...)` 
- [ ] `MauiSyncService.SyncAsync()` tiene la llamada equivalente
- [ ] `SyncAsync()` llama a `IEntityRepository<{Entidad}>.InvalidateCache()` tras sincronizar

### 11. UI
- [ ] Existe página de lista en `Entcuentro.UI/Pages/`
- [ ] Existe página de formulario en `Entcuentro.UI/Pages/`
- [ ] Las páginas tienen `[Authorize]` o `[Authorize(Roles = "...")]`
- [ ] Los links están en `NavMenu.razor` (Web y MAUI)

---

## Informe final

Presenta los resultados agrupados así:

**✅ Completo y correcto** — lista de puntos ok  
**⚠️ Existe pero necesita corrección** — lista con descripción del problema  
**❌ Falta por crear** — lista de lo que hay que hacer

Si hay elementos ❌ o ⚠️, pregunta al usuario si quiere que los corrijas o crees. Si son pocos, ofrece hacerlo en ese momento. Si son muchos, sugiere ejecutar `/nueva-entidad` para esa entidad.

---

## Notas sobre la revisión

- Leer los archivos reales, no suponer su contenido.
- Prestar especial atención a la **ruta del controlador** vs la convención de `IEntityRepository<T>`: si no coinciden, `Repo.GetAllAsync()` y `Repo.GetByIdAsync()` fallarán silenciosamente (devuelven vacío en lugar de error).
- Un campo `IsSynced = false` en Update es importante: indica que hay un cambio local que debe subirse al servidor en la próxima sincronización.
- No confundir `I{Entidad}Service` (servicio de negocio, lado servidor) con `IEntityRepository<{Entidad}>` (repositorio con caché, lado cliente).
