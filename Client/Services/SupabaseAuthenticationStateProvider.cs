using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorBasic.Client.Services;

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
            ? new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, _authenticationService.Email ?? "user")
            }, "supabase")
            : new System.Security.Claims.ClaimsIdentity();

        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
   
}