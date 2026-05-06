using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Interfaces;
using MyApp.Shared.DTOs;
using MyApp.Shared.Enums;

namespace MyApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = RolNombre.Administrador)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        => Ok(await userService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = RolNombre.Administrador)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        try
        {
            var user = await userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UpdateUserRequest request)
    {
        try
        {
            var user = await userService.UpdateAsync(id, request);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RolNombre.Administrador)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await userService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/roles/{rol}")]
    [Authorize(Roles = RolNombre.Administrador)]
    public async Task<IActionResult> AssignRol(string id, string rol)
    {
        await userService.AssignRolAsync(id, rol);
        return NoContent();
    }

    [HttpDelete("{id}/roles/{rol}")]
    [Authorize(Roles = RolNombre.Administrador)]
    public async Task<IActionResult> RemoveRol(string id, string rol)
    {
        await userService.RemoveRolAsync(id, rol);
        return NoContent();
    }
}
