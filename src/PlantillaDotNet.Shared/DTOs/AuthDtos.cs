namespace PlantillaDotNet.Shared.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string UserName);

public record AuthResponse(string AccessToken, string Email, string UserName);
