using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entcuentro.Application.Interfaces;
using Entcuentro.Shared.DTOs;
using Entcuentro.Shared.Models;

namespace Entcuentro.Api.Controllers;

// Clase base para controladores de sincronización.
// Al añadir una entidad (ej. TodoItem : SyncableEntity), crear:
//   public class TodoSyncController : SyncControllerBase<TodoItem> { ... }
[ApiController]
[Authorize]
public abstract class SyncControllerBase<T>(IServerSyncService syncService) : ControllerBase
    where T : SyncableEntity
{
    [HttpGet("pull")]
    public async Task<ActionResult<SyncPullResponse<T>>> Pull([FromQuery] DateTime since)
    {
        var serverChanges = await syncService.PullAsync<T>(since);
        return Ok(new SyncPullResponse<T>(serverChanges, DateTime.UtcNow));
    }

    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] SyncPushRequest<T> request)
    {
        await syncService.PushAsync(request.Changes);
        return Ok();
    }
}
