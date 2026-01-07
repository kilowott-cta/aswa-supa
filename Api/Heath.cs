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
        _ = await _supabaseClient.InitializeAsync();
        _supabaseClient.Auth.ClearStateChangedListeners();
        var session =  await _supabaseClient.Auth.SetSession(token, token);
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
        ClaimsPrincipal user = req.HttpContext.User;
        
        _logger.LogInformation("User: {User}", user.Identity?.Name);
        return new OkObjectResult("Welcome to Functions!");
    }

    [Function("projects")]
    public async Task<IActionResult> GetProjects([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        if (!await IsAuthorized(req))
        {
            return new UnauthorizedResult();
        }
        var results = await _supabaseClient
            .From<DomainBasic.Models.Dbo.Project>().Get();

        var projects = results.Models.Select(p => p.ToDto());

        return new OkObjectResult(projects);
    }
}