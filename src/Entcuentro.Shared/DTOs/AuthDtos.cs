namespace Entcuentro.Shared.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string UserName, string? Localidad);

public record AuthResponse(string AccessToken, string Email, string UserName);
