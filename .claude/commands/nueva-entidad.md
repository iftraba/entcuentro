Eres un asistente especializado en este proyecto PlantillaDotNet. Tu tarea es crear una nueva entidad completa en la base de datos siguiendo EXACTAMENTE los patrones establecidos.

## Antes de empezar

Si el usuario no ha indicado el nombre de la entidad y sus campos, pregúntale:
1. **Nombre** de la entidad (singular, PascalCase, en español o inglés)
2. **Campos** adicionales (nombre, tipo C#, si es requerido). Los campos `Id`, `UpdatedAt`, `IsSynced`, `IsDeleted` vienen de `SyncableEntity` y NO deben repetirse.

Muestra un resumen de todos los archivos que vas a crear/modificar y pide confirmación antes de empezar.

---

## Patrón a seguir (sigue este orden exacto)

### 1. Modelo — `src/PlantillaDotNet.Shared/Models/{Entidad}.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace PlantillaDotNet.Shared.Models;

public class {Entidad} : SyncableEntity
{
    // campos del usuario aquí
    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;
    // ... resto de campos
}
```

- Siempre heredar de `SyncableEntity` (proporciona Id, UpdatedAt, IsSynced, IsDeleted)
- Usar DataAnnotations para validación
- Strings con valor por defecto `string.Empty`

### 2. DTOs — `src/PlantillaDotNet.Shared/DTOs/{Entidad}Dtos.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace PlantillaDotNet.Shared.DTOs;

public record Create{Entidad}Request(
    [Required][MaxLength(200)] string Nombre
    // ... campos requeridos para crear
);

public record Update{Entidad}Request(
    Guid Id,
    [Required][MaxLength(200)] string Nombre
    // ... campos actualizables
);

public record {Entidad}Response(
    Guid Id,
    string Nombre,
    // ... todos los campos para devolver al cliente
    DateTime UpdatedAt
);
```

### 3. Interfaz de servicio — `src/PlantillaDotNet.Application/Interfaces/I{Entidad}Service.cs`

```csharp
using PlantillaDotNet.Shared.DTOs;
using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Application.Interfaces;

public interface I{Entidad}Service
{
    Task<IEnumerable<{Entidad}Response>> GetAllAsync();
    Task<{Entidad}Response?> GetByIdAsync(Guid id);
    Task<{Entidad}Response> CreateAsync(Create{Entidad}Request request);
    Task<{Entidad}Response> UpdateAsync(Update{Entidad}Request request);
    Task DeleteAsync(Guid id);
}
```

### 4. Implementación — `src/PlantillaDotNet.Infrastructure/Services/{Entidad}Service.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PlantillaDotNet.Application.Interfaces;
using PlantillaDotNet.Infrastructure.Data;
using PlantillaDotNet.Shared.DTOs;
using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Infrastructure.Services;

public class {Entidad}Service(AppDbContext db) : I{Entidad}Service
{
    public async Task<IEnumerable<{Entidad}Response>> GetAllAsync() =>
        await db.{Entidades}
            .Where(e => !e.IsDeleted)
            .Select(e => ToResponse(e))
            .ToListAsync();

    public async Task<{Entidad}Response?> GetByIdAsync(Guid id)
    {
        var entity = await db.{Entidades}.FindAsync(id);
        return entity is null || entity.IsDeleted ? null : ToResponse(entity);
    }

    public async Task<{Entidad}Response> CreateAsync(Create{Entidad}Request request)
    {
        var entity = new {Entidad}
        {
            // mapear campos del request
            Nombre = request.Nombre,
            UpdatedAt = DateTime.UtcNow
        };
        db.{Entidades}.Add(entity);
        await db.SaveChangesAsync();
        return ToResponse(entity);
    }

    public async Task<{Entidad}Response> UpdateAsync(Update{Entidad}Request request)
    {
        var entity = await db.{Entidades}.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"{Entidad} {request.Id} no encontrada.");
        // actualizar campos
        entity.Nombre = request.Nombre;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsSynced = false;
        await db.SaveChangesAsync();
        return ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await db.{Entidades}.FindAsync(id)
            ?? throw new KeyNotFoundException($"{Entidad} {id} no encontrada.");
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static {Entidad}Response ToResponse({Entidad} e) =>
        new(e.Id, e.Nombre, /* ... resto de campos ... */ e.UpdatedAt);
}
```

### 5. DbContext — `src/PlantillaDotNet.Infrastructure/Data/AppDbContext.cs`

Añadir la propiedad al `AppDbContext`:
```csharp
public DbSet<{Entidad}> {Entidades} { get; set; }
```

### 6. Registro DI — `src/PlantillaDotNet.Infrastructure/DependencyInjection.cs`

Añadir en el método de registro de servicios:
```csharp
services.AddScoped<I{Entidad}Service, {Entidad}Service>();
```

### 7. Migración EF

Ejecutar:
```bash
dotnet ef migrations add Add{Entidad} --project src/PlantillaDotNet.Infrastructure --startup-project src/PlantillaDotNet.Api
```

### 8. Controlador API — `src/PlantillaDotNet.Api/Controllers/{Entidades}Controller.cs`

**IMPORTANTE:** La ruta base DEBE ser `/api/{entidades_plural_minúsculas}` para que `IEntityRepository<{Entidad}>` la encuentre automáticamente en el cliente (convención `typeof(T).Name.ToLower() + "s"`).

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantillaDotNet.Application.Interfaces;
using PlantillaDotNet.Shared.DTOs;
using PlantillaDotNet.Shared.Models;

namespace PlantillaDotNet.Api.Controllers;

[ApiController]
[Route("api/{entidades}")]
[Authorize]
public class {Entidades}Controller(I{Entidad}Service service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // CRÍTICO para IEntityRepository<{Entidad}>:
    // GET /api/{entidades}          → GetAll
    // GET /api/{entidades}/{id}     → GetById
    // Estas dos rutas son las que usa CachedRepository<{Entidad}> automáticamente.

    [HttpPost]
    public async Task<IActionResult> Create(Create{Entidad}Request request)
    {
        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Update{Entidad}Request request)
    {
        if (id != request.Id) return BadRequest();
        return Ok(await service.UpdateAsync(request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
```

### 9. Verificar compilación

```bash
dotnet build PlantillaDotNet.slnx
```

Si compila correctamente, el cliente (Web y MAUI) ya puede usar:
```razor
@inject IEntityRepository<{Entidad}> Repo
```
y el flujo caché → BD local → API funciona automáticamente sin ningún registro adicional.

---

## Notas importantes

- La caché se invalida automáticamente en `SaveAsync`/`DeleteAsync`. Llamar a `Repo.InvalidateCache()` manualmente solo es necesario tras una sincronización con `ISyncService.SyncAsync()`.
- Si la entidad NO necesita soporte offline, puede heredar de una clase base simple en lugar de `SyncableEntity`. En ese caso, `IEntityRepository<T>` seguirá funcionando pero solo usará las capas 1 (memoria) y 3 (API), sin BD local.
- Los borrados son siempre **soft delete** (`IsDeleted = true`), nunca `DELETE` físico.
- El controlador devuelve `{Entidad}Response` (DTO), nunca la entidad de dominio directamente.
