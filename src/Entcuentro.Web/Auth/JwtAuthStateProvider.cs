using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Entcuentro.UI.Auth;

namespace Entcuentro.Web.Auth;

public class JwtAuthStateProvider(IJSRuntime jsRuntime) : AuthenticationStateProvider, ITokenManager
{
    private const string TokenKey = "authToken";
    private const string RememberedEmailKey = "rememberedEmail";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return Unauthenticated();

        var identity = ParseClaimsFromJwt(token);
        if (identity is null)
            return Unauthenticated();

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyAuthenticatedAsync(string token)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        var identity = ParseClaimsFromJwt(token);
        var user = new ClaimsPrincipal(identity ?? new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task NotifyLoggedOutAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Unauthenticated()));
    }

    public async Task<string?> GetRememberedEmailAsync()
    {
        var email = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", RememberedEmailKey);
        return string.IsNullOrEmpty(email) ? null : email;
    }

    public async Task SaveRememberedEmailAsync(string email)
        => await jsRuntime.InvokeVoidAsync("localStorage.setItem", RememberedEmailKey, email);

    public async Task RemoveRememberedEmailAsync()
        => await jsRuntime.InvokeVoidAsync("localStorage.removeItem", RememberedEmailKey);

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
