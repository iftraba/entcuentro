using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using PlantillaDotNet.Shared.Enums;

namespace PlantillaDotNet.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(9)]
    public string? Dni { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    public Sexo Sexo { get; set; } = Sexo.NoEspecificado;

    public DateOnly? FechaNacimiento { get; set; }

    public int? Edad => FechaNacimiento.HasValue
        ? CalcularEdad(FechaNacimiento.Value)
        : null;

    private static int CalcularEdad(DateOnly fechaNacimiento)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento > hoy.AddYears(-edad)) edad--;
        return edad;
    }
}
