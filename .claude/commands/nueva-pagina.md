Eres un asistente especializado en este proyecto Entcuentro. Tu tarea es crear una nueva página Blazor compartida siguiendo EXACTAMENTE los patrones del proyecto.

## Antes de empezar

Si el usuario no ha indicado los parámetros, pregúntale:
1. **Entidad** que gestionará la página (ej. `Producto`) — debe existir ya `IEntityRepository<T>` para ella
2. **Tipo de página** a generar:
   - `lista` — tabla con todos los registros + botones crear/editar/eliminar
   - `formulario` — crear y editar en la misma página (modo create si no hay Id, modo edit si hay Id en query string)
   - `ambas` — genera las dos páginas (lo más habitual)
3. **Ruta** (ej. `/productos`, `/productos/editar`)
4. **Título** visible (ej. `Productos`, `Gestión de productos`)
5. **Rol requerido** (opcional): dejar en blanco para cualquier usuario autenticado, o indicar `Administrador`

Muestra un resumen de archivos a crear y pide confirmación antes de empezar.

---

## Patrón a seguir

Todas las páginas van en `src/Entcuentro.UI/Pages/` (compartidas entre Web y MAUI).

### Página de lista — `src/Entcuentro.UI/Pages/{Entidades}Page.razor`

```razor
@page "/{entidades}"
@attribute [Authorize]          @* o [Authorize(Roles = "Administrador")] si es solo admin *@
@using Entcuentro.Shared.Models
@inject IEntityRepository<{Entidad}> Repo
@inject NavigationManager Nav

<PageTitle>{Titulo}</PageTitle>

<div class="d-flex justify-content-between align-items-center mb-3">
    <h2 class="mb-0">{Titulo}</h2>
    <button class="btn btn-primary" @onclick="Crear">
        <span class="bi bi-plus-lg me-1"></span> Nuevo
    </button>
</div>

@if (isLoading)
{
    <div class="text-center py-5">
        <div class="spinner-border text-primary" role="status"></div>
    </div>
}
else if (errorMessage is not null)
{
    <div class="alert alert-danger">@errorMessage</div>
}
else if (!items.Any())
{
    <div class="alert alert-info">No hay registros todavía.</div>
}
else
{
    <div class="table-responsive">
        <table class="table table-hover align-middle">
            <thead class="table-light">
                <tr>
                    <th>Nombre</th>
                    @* añadir columnas según los campos de la entidad *@
                    <th class="text-end">Acciones</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in items)
                {
                    <tr>
                        <td>@item.Nombre</td>
                        @* añadir celdas según los campos *@
                        <td class="text-end">
                            <button class="btn btn-sm btn-outline-primary me-1"
                                    @onclick="() => Editar(item.Id)">
                                Editar
                            </button>
                            <button class="btn btn-sm btn-outline-danger"
                                    @onclick="() => EliminarAsync(item.Id)">
                                Eliminar
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

@code {
    private List<{Entidad}> items = [];
    private bool isLoading = true;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            items = (await Repo.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al cargar los datos: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void Crear() => Nav.NavigateTo("/{entidades}/editar");
    private void Editar(Guid id) => Nav.NavigateTo($"/{entidades}/editar?id={id}");

    private async Task EliminarAsync(Guid id)
    {
        try
        {
            await Repo.DeleteAsync(id);
            items = (await Repo.GetAllAsync()).ToList();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al eliminar: {ex.Message}";
        }
    }
}
```

### Página de formulario — `src/Entcuentro.UI/Pages/{Entidades}EditPage.razor`

```razor
@page "/{entidades}/editar"
@attribute [Authorize]
@using Entcuentro.Shared.Models
@inject IEntityRepository<{Entidad}> Repo
@inject NavigationManager Nav

<PageTitle>@(isNew ? "Nuevo {entidad}" : "Editar {entidad}")</PageTitle>
<h2>@(isNew ? "Nuevo {entidad}" : "Editar {entidad}")</h2>

@if (isLoading)
{
    <div class="spinner-border text-primary" role="status"></div>
}
else
{
    <div class="card shadow-sm" style="max-width: 600px;">
        <div class="card-body">
            <EditForm Model="model" OnValidSubmit="GuardarAsync">
                <DataAnnotationsValidator />

                @if (errorMessage is not null)
                {
                    <div class="alert alert-danger">@errorMessage</div>
                }

                <div class="mb-3">
                    <label class="form-label fw-semibold">Nombre</label>
                    <InputText @bind-Value="model.Nombre" class="form-control" />
                    <ValidationMessage For="() => model.Nombre" class="text-danger small" />
                </div>
                @* añadir campos del formulario según la entidad *@

                <div class="d-flex gap-2 mt-4">
                    <button type="submit" class="btn btn-primary" disabled="@isSaving">
                        @if (isSaving)
                        {
                            <span class="spinner-border spinner-border-sm me-1"></span>
                        }
                        Guardar
                    </button>
                    <button type="button" class="btn btn-outline-secondary"
                            @onclick="Cancelar">
                        Cancelar
                    </button>
                </div>
            </EditForm>
        </div>
    </div>
}

@code {
    [SupplyParameterFromQuery] private Guid? Id { get; set; }

    private {Entidad} model = new();
    private bool isNew = true;
    private bool isLoading = true;
    private bool isSaving;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (Id.HasValue)
        {
            isNew = false;
            var existing = await Repo.GetByIdAsync(Id.Value);
            if (existing is not null) model = existing;
        }
        isLoading = false;
    }

    private async Task GuardarAsync()
    {
        isSaving = true;
        errorMessage = null;
        try
        {
            if (isNew) model.Id = Guid.NewGuid();
            model.UpdatedAt = DateTime.UtcNow;
            await Repo.SaveAsync(model);
            Nav.NavigateTo("/{entidades}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void Cancelar() => Nav.NavigateTo("/{entidades}");
}
```

### Añadir los links al NavMenu

Recuerda añadir los enlaces en `src/Entcuentro.Web/Layout/NavMenu.razor` y en `src/Entcuentro.Maui/Components/Layout/NavMenu.razor`:

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="{entidades}">
        <span class="bi bi-list-ul" aria-hidden="true"></span> {Titulo}
    </NavLink>
</div>
```

Si la página es solo para administradores, envolverla en `<AuthorizeView Roles="Administrador">`.

---

## Notas importantes

- Las páginas van en `Entcuentro.UI` para que funcionen en Web Y MAUI sin duplicar código.
- `[SupplyParameterFromQuery]` es la forma correcta de leer parámetros de la URL en Blazor .NET 8+.
- El borrado llama a `Repo.DeleteAsync()` que hace soft-delete; la lista recarga desde caché que ya excluye `IsDeleted`.
- No añadir confirmación de borrado con `alert()` de JS; usar un modal Bootstrap o simplemente un segundo botón de confirmación inline si el usuario lo pide.
- Compilar siempre tras crear las páginas: `dotnet build Entcuentro.slnx`
