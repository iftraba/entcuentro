namespace Entcuentro.UI.Auth;

public interface ITokenManager
{
    Task NotifyAuthenticatedAsync(string token);
    Task NotifyLoggedOutAsync();
    Task<string?> GetRememberedEmailAsync();
    Task SaveRememberedEmailAsync(string email);
    Task RemoveRememberedEmailAsync();
}
