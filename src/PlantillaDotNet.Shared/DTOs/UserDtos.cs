using PlantillaDotNet.Shared.Enums;

namespace PlantillaDotNet.Shared.DTOs;

public record UserDto(
    string Id,
    string Email,
    string UserName,
    string Nombre,
    string Apellidos,
    string? Direccion,
    string? Dni,
    string? Telefono,
    Sexo Sexo,
    DateOnly? FechaNacimiento,
    int? Edad,
    IEnumerable<string> Roles
);

public record CreateUserRequest(
    string Email,
    string Password,
    string UserName,
    string Nombre,
    string Apellidos,
    string? Direccion,
    string? Dni,
    string? Telefono,
    Sexo Sexo,
    DateOnly? FechaNacimiento,
    string Rol
);

public record UpdateUserRequest(
    string Nombre,
    string Apellidos,
    string? Direccion,
    string? Dni,
    string? Telefono,
    Sexo Sexo,
    DateOnly? FechaNacimiento
);
