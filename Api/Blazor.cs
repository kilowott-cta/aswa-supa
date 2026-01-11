using System.Security.Claims;
using System.Text.Json;
using DomainBasic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FunctionBasic;

public class Blazor
{
    private readonly ILogger<Blazor> _logger;
    private readonly IConfiguration _configuration;
    private readonly Supabase.Client _supabaseClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive= true
    };

    private async Task<bool> IsAuthorized(HttpRequest req)
    {
        var token = req.Headers["Authorization"].ToString().Replace("Bearer ", "");
        _supabaseClient.Auth.ClearStateChangedListeners();
        var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
        return session.User != null;
    }

    public Blazor(ILogger<Blazor> logger, IConfiguration configuration, Supabase.Client supabaseClient)
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

    [Function("get-projects")]
    public async Task<IActionResult> GetProjects([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            var token = req.Headers["x-Authorization"].ToString().Replace("Bearer ", "");
            var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
            
            var results = await _supabaseClient
                .From<DomainBasic.Models.Dbo.Project>().Get();

            var projects = results.Models.Select(p => p.ToDtoFromDbo());

            return new OkObjectResult(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new BadRequestResult();
        }
    }

    [Function("post-projects")]
    public async Task<IActionResult> PostProjects([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            using var reader = new StreamReader(req.Body);
            var requestBody= await reader.ReadToEndAsync();
            var items = JsonSerializer.Deserialize<List<DomainBasic.Models.Dto.Project>>(
                requestBody, _jsonSerializerOptions
            );

            var token = req.Headers["x-Authorization"].ToString().Replace("Bearer ", "");
            var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
            
            var projects = items?.Select(i => i.ToDboFromDto()).ToList();
            if (projects is null) return new OkObjectResult(projects?.Count);
            var results = await _supabaseClient
                .From<DomainBasic.Models.Dbo.Project>().Upsert(projects);

            return new OkObjectResult(projects?.Count);
        }
        catch(Exception ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function("uploads")]
    public async Task<IActionResult> FileUploads([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            var file = req.Form.Files["File"];
            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("No file uploaded.");
            }
            using Stream stream = file.OpenReadStream();
            await ProcessFile();
            return new OkObjectResult($"File {file.FileName} uploaded successfully");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
            return new BadRequestObjectResult($"Error: {ex.Message}");
        }
    }

    async Task ProcessFile() => await Task.Run(() =>
    {
        _logger.LogInformation("Processing file");
    });
}