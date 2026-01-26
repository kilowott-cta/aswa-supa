using System.Net.Http.Headers;
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
    private static readonly HttpClient httpClient = new HttpClient();

    public Health(ILogger<Health> logger, Supabase.Client supabaseClient)
    {
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    [Function("greeting")]
    public async Task<IActionResult> PingPong([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _ = await _supabaseClient.InitializeAsync();
        //var url = _supabaseClient.Postgrest.BaseUrl;
        ClaimsPrincipal user = req.HttpContext.User;

        return new OkObjectResult($"Welcome {user?.Identity?.Name}");
    }

    [Function("openai")]
    public static async Task<IActionResult> TestOpenAI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new BadRequestObjectResult("❌ Missing OPENAI_API_KEY environment variable");
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            return new OkObjectResult(new
            {
                status = (int)response.StatusCode,
                ok = response.IsSuccessStatusCode,
                body
            });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error calling OpenAI API");
            return new ObjectResult($"❌ Error: {ex.Message}") { StatusCode = 500 };
        }
    }
}
