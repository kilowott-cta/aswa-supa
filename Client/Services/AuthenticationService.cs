using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorBasic.Services;

public class SupabaseSession
{
    public string? AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
}

public class AuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;

    private SupabaseSession? _session;
    public event Action? OnAuthStateChanged;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IJSRuntime jsRuntime,
        IConfiguration configuration,
        NavigationManager navigationManager)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _navigationManager = navigationManager;
    }
    public string SupabaseUrl
        => _configuration["Supabase:Url"] ?? _configuration["SUPABASE_URL"] ?? string.Empty;
    public string SupabaseKey
        => _configuration["Supabase:Key"] ?? _configuration["SUPABASE_KEY"] ?? string.Empty;

    public bool IsAuthenticated => _session != null && !string.IsNullOrEmpty(_session.AccessToken);
    public string? AccessToken => _session?.AccessToken;
    public string? RefreshToken => _session?.RefreshToken;
    public string? Email => _session?.Email;

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "supabase_session");
            if (!string.IsNullOrEmpty(json))
            {
                _session = JsonSerializer.Deserialize<SupabaseSession>(json);
            }
        }
        catch
        {
            _session = null;
        }
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> SignInWithPasswordAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseKey))
        {
            return false;
        }
        var client = _httpClientFactory.CreateClient("SupabaseClient");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseUrl}/auth/v1/token?grant_type=password");
        var body = JsonSerializer.Serialize(new
        {
            email,
            password
        });
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        request.Headers.Add("apikey", SupabaseKey);
        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;
        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("access_token", out var at))
        {
            var access = at.GetString();
            var refresh = root.GetProperty("refresh_token").GetString();
            _session = new SupabaseSession { AccessToken = access, RefreshToken = refresh, Email = email };
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", JsonSerializer.Serialize(_session));
            OnAuthStateChanged?.Invoke();
            return true;
        }
        return false;
    }

    public async Task<(bool Success, string? Message)> SignUpWithPasswordAsync(string email, string password)
    {
        if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseKey))
        {
            return (false, "Supabase configuration is missing.");
        }
        var client = _httpClientFactory.CreateClient("SupabaseClient");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseUrl}/auth/v1/signup");
        var body = JsonSerializer.Serialize(new
        {
            email,
            password
        });
        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        request.Headers.Add("apikey", SupabaseKey);
        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("message", out var msg))
            {
                return (false, msg.GetString() ?? "Sign-up failed.");
            }
            return (false, responseBody);
        }
        try
        {
            using var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var at))
            {
                var access = at.GetString();
                var refresh = jsonDoc.RootElement.GetProperty("refresh_token").GetString();
                _session = new SupabaseSession { AccessToken = access, RefreshToken = refresh, Email = email };
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", JsonSerializer.Serialize(_session));
                OnAuthStateChanged?.Invoke();
                return (true, "Sign-up successful and signed in.");
            }
            return (true, "Sign-up successful. Please check your email for confirmation.");
        }
        catch
        {
            return (true, null);
        }
    }

    public async Task SetSessionFromFragmentAsync(string fragment)
    {
        if (string.IsNullOrEmpty(fragment)) return;
        var parts = fragment.TrimStart('#').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var dict = parts.Select(p => p.Split('=', 2)).Where(a => a.Length == 2)
                        .ToDictionary(a => a[0], a => Uri.UnescapeDataString(a[1]));
        if (dict.TryGetValue("access_token", out var access))
        {
            dict.TryGetValue("refresh_token", out var refresh);
            _session = new SupabaseSession { AccessToken = access, RefreshToken = refresh };
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "supabase_session", JsonSerializer.Serialize(_session));
            OnAuthStateChanged?.Invoke();
        }
    }

    public async Task SignOutAsync()
    {
        _session = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "supabase_session");
        OnAuthStateChanged?.Invoke();
        _navigationManager.NavigateTo("/");
    }

    public string GetOAuthUrl(string provider)
    {
        var redirect = _navigationManager.BaseUri.TrimEnd('/') + "/authentication-callback";
        var url = $"{SupabaseUrl}/auth/v1/authorize?provider={provider}&redirect_to={Uri.EscapeDataString(redirect)}";
        return url;
    }
}