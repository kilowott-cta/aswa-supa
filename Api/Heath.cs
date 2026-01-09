using System.Security.Claims;
using DomainBasic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FunctionBasic;

public class Health
{
    private readonly ILogger<Health> _logger;
    private readonly IConfiguration _configuration;
    private readonly Supabase.Client _supabaseClient;

    private async Task<bool> IsAuthorized(HttpRequest req)
    {
        var token = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
        _supabaseClient.Auth.ClearStateChangedListeners();
        var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
        return session.User != null;
    }

    public Health(ILogger<Health> logger, IConfiguration configuration, Supabase.Client supabaseClient)
    {
        _logger = logger;
        _configuration = configuration;
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
        try
        {
            var pass = _configuration["SUPABASE_PASS"] ?? string.Empty;
            var token = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
            var user = await _supabaseClient.Auth.GetUser(token);
            if (user is null || string.IsNullOrEmpty(user?.Email))
            {
                return new BadRequestResult();
            }
            //var session = await _supabaseClient.Auth.RetrieveSessionAsync()
            session = await _supabaseClient.Auth.SignIn(user.Email, pass);
            
            var results = await _supabaseClient
                .From<DomainBasic.Models.Dbo.Project>().Get();

            var projects = results.Models.Select(p => p.ToDto());

            return new OkObjectResult(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new BadRequestResult();
        }

    }
}