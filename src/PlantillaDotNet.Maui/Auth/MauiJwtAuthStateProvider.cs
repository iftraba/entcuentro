using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PlantillaDotNet.UI.Auth;

namespace PlantillaDotNet.Maui.Auth;

public class MauiJwtAuthStateProvider : AuthenticationStateProvider, ITokenManager
{
    private const string TokenKey = "authToken";
    private const string RememberedEmailKey = "rememberedEmail";

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = SecureStorage.Default.GetAsync(TokenKey).GetAwaiter().GetResult();

        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(Unauthenticated());

        var identity = ParseClaimsFromJwt(token);
        if (identity is null)
            return Task.FromResult(Unauthenticated());

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public async Task NotifyAuthenticatedAsync(string token)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
        var identity = ParseClaimsFromJwt(token);
        var user = new ClaimsPrincipal(identity ?? new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public Task NotifyLoggedOutAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Unauthenticated()));
        return Task.CompletedTask;
    }

    public Task<string?> GetRememberedEmailAsync()
    {
        var email = Preferences.Default.Get(RememberedEmailKey, string.Empty);
        return Task.FromResult<string?>(string.IsNullOrEmpty(email) ? null : email);
    }

    public Task SaveRememberedEmailAsync(string email)
    {
        Preferences.Default.Set(RememberedEmailKey, email);
        return Task.CompletedTask;
    }

    public Task RemoveRememberedEmailAsync()
    {
        Preferences.Default.Remove(RememberedEmailKey);
        return Task.CompletedTask;
    }

    private static AuthenticationState Unauthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static ClaimsIdentity? ParseClaimsFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            return new ClaimsIdentity(jwt.Claims, "jwt");
        }
        catch
        {
            return null;
        }
    }
}
