using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Identity;
using MyApp.Shared.DTOs;

namespace MyApp.Infrastructure.Services;

public class UserService(
    UserManager<AppIdentityUser> userManager) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(ToDto(user, roles));
        }
        return result;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = new AppIdentityUser
        {
            UserName = request.UserName,
            Email = request.Email,
            Nombre = request.Nombre,
            Apellidos = request.Apellidos,
            Direccion = request.Direccion,
            Dni = request.Dni,
            Telefono = request.Telefono,
            Sexo = request.Sexo,
            FechaNacimiento = request.FechaNacimiento
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, request.Rol);

        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<UserDto> UpdateAsync(string id, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuario {id} no encontrado.");

        user.Nombre = request.Nombre;
        user.Apellidos = request.Apellidos;
        user.Direccion = request.Direccion;
        user.Dni = request.Dni;
        user.Telefono = request.Telefono;
        user.Sexo = request.Sexo;
        user.FechaNacimiento = request.FechaNacimiento;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task DeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"Usuario {id} no encontrado.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task AssignRolAsync(string userId, string rol)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");
        await userManager.AddToRoleAsync(user, rol);
    }

    public async Task RemoveRolAsync(string userId, string rol)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");
        await userManager.RemoveFromRoleAsync(user, rol);
    }

    private static UserDto ToDto(AppIdentityUser user, IEnumerable<string> roles) => new(
        Id: user.Id,
        Email: user.Email ?? string.Empty,
        UserName: user.UserName ?? string.Empty,
        Nombre: user.Nombre,
        Apellidos: user.Apellidos,
        Direccion: user.Direccion,
        Dni: user.Dni,
        Telefono: user.Telefono,
        Sexo: user.Sexo,
        FechaNacimiento: user.FechaNacimiento,
        Edad: user.Edad,
        Roles: roles
    );
}
