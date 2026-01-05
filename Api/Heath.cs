using System.Security.Claims;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Supabase;

namespace Function.Basic;

public class Health
{
    private readonly ILogger<Health> _logger;
    private readonly Client _supabaseClient;

    public Health(ILogger<Health> logger, Client supabaseClient)
    {
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    [Function("pingpong")]
    public IActionResult PingPong([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        ClaimsPrincipal user = req.HttpContext.User;
        _logger.LogInformation("User: {User}", user.Identity?.Name);
        return new OkObjectResult("Welcome to Functions!");
    }

    [Function("supabasehealth")]
    public async Task<IActionResult> SupabaseHealth([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("Checking Supabase health.");
        _ = await _supabaseClient.InitializeAsync();
        var url = _supabaseClient.Postgrest.BaseUrl;
        var email = _supabaseClient.Auth.CurrentUser?.Email ?? "No user authenticated";
        return new OkObjectResult($"Supabase client initialized. Current user email: {email}. URL: {url}");
    }
}