using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorBasic.Services;

public class SupabaseAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationService _authenticationService;

    public SupabaseAuthenticationStateProvider(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        _authenticationService.OnAuthStateChanged += Notify;
    }

    private void Notify() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = _authenticationService.IsAuthenticated
            ? new ClaimsIdentity(ParseClaimsFromJwt(_authenticationService.AccessToken ?? ""), "jwt")
            : new ClaimsIdentity();

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));  
    }

    

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);

        using var document = JsonDocument.Parse(jsonBytes);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    yield return new Claim(property.Name, item.ToString());
                }
            }
            else
            {
                yield return new Claim(property.Name, property.Value.ToString());
            }
        }
    }
    private static byte[] ParseBase64WithoutPadding(string base64Url)
    {
        // Convert Base64URL → Base64
        string base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

}