using Microsoft.AspNetCore.Identity;
using MyApp.Infrastructure.Identity;
using MyApp.Shared.Enums;

namespace MyApp.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = [RolNombre.Administrador, RolNombre.Usuario];

        foreach (var rol in roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }
    }

    public static async Task SeedUsersAsync(UserManager<AppIdentityUser> userManager)
    {
        await CreateUserIfNotExists(userManager, new AppIdentityUser
        {
            UserName = "admin",
            Email = "admin@myapp.com",
            EmailConfirmed = true,
            Nombre = "Admin",
            Apellidos = "App",
            Dni = "00000000T",
            Telefono = "600000001",
            Direccion = "Calle Mayor 1, Madrid",
            Sexo = Sexo.NoEspecificado,
            FechaNacimiento = new DateOnly(1985, 1, 15)
        }, "Admin1234!", RolNombre.Administrador);

        await CreateUserIfNotExists(userManager, new AppIdentityUser
        {
            UserName = "juan.garcia",
            Email = "juan@myapp.com",
            EmailConfirmed = true,
            Nombre = "Juan",
            Apellidos = "García Pérez",
            Dni = "12345678Z",
            Telefono = "611222333",
            Direccion = "Avenida de la Paz 23, Barcelona",
            Sexo = Sexo.Masculino,
            FechaNacimiento = new DateOnly(1990, 6, 20)
        }, "Usuario1234!", RolNombre.Usuario);

        await CreateUserIfNotExists(userManager, new AppIdentityUser
        {
            UserName = "maria.lopez",
            Email = "maria@myapp.com",
            EmailConfirmed = true,
            Nombre = "María",
            Apellidos = "López Sánchez",
            Dni = "98765432X",
            Telefono = "622333444",
            Direccion = "Calle del Sol 7, Sevilla",
            Sexo = Sexo.Femenino,
            FechaNacimiento = new DateOnly(1995, 3, 8)
        }, "Usuario1234!", RolNombre.Usuario);
    }

    private static async Task CreateUserIfNotExists(
        UserManager<AppIdentityUser> userManager,
        AppIdentityUser user,
        string password,
        string rol)
    {
        if (await userManager.FindByEmailAsync(user.Email!) is not null)
            return;

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, rol);
    }
}
