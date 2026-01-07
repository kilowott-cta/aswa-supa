using System.Security.Claims;
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
        var session =  await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
        return session.User != null;
    }

    public Health(ILogger<Health> logger, Supabase.Client supabaseClient)
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

    [Function("projects")]
    public async Task<IActionResult> ProjectsCount([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        if (!await IsAuthorized(req))
        {
            return new UnauthorizedResult();
        }
        var results = await _supabaseClient
            .From<DataBasic.Dbo.Project>().Get();

        var projects = results.Models.Select(p => new DataBasic.Dto.Project
        {
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectName,
            Stage = p.Stage,
            Status = p.Status,
            ClientName = p.ClientName,
            AccountManager = p.AccountManager,
            Designers= p.Designers,
            Architects = p.Architects,
            Analysts = p.Analysts,
            Tags = p.Tags,
            SoldHours = p.SoldHours,
            BallparkHours = p.BallparkHours,
            Owner = p.Owner,
            PresalesPriority = p.PresalesPriority,
            Skillsets = p.Skillsets,
            UpdatedAt = p.UpdatedAt,
            CreatedAt = p.CreatedAt
        });
        return new OkObjectResult(projects);
    }
}