using System.Security.Claims;
using DomainBasic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionBasic;

public class Health
{
    private readonly ILogger<Health> _logger;
    private readonly Supabase.Client _supabaseClient;

    private async Task<bool> IsAuthorized(HttpRequest req)
    {
        var token = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
        _supabaseClient.Auth.ClearStateChangedListeners();
        var session =  await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
        return session.User != null;
    }

    public Health(ILogger<Health> logger, Supabase.Client supabaseClient)
    {
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    [Function("pingpong")]
    public async Task<IActionResult> PingPong([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _ = await _supabaseClient.InitializeAsync();
        var url = _supabaseClient.Postgrest.BaseUrl;
        ClaimsPrincipal user = req.HttpContext.User;
        
        return new OkObjectResult($"Welcome {user?.Identity?.Name} to Functions with {url}.");
    }

    [Function("projects")]
    public async Task<IActionResult> GetProjects([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        _ = await _supabaseClient.InitializeAsync();
        var token = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var session =  await _supabaseClient.Auth.SetSession(token, token);
        await _supabaseClient.Auth.RefreshSession();
        var results = await _supabaseClient
            .From<DomainBasic.Models.Dbo.Project>().Get();

        var projects = results.Models.Select(p => p.ToDto());

        return new OkObjectResult(projects);
    }
}