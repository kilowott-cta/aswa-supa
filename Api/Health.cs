using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
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
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public Health(ILogger<Health> logger, IConfiguration configuration, Supabase.Client supabaseClient)
    {
        _logger = logger;
        _configuration = configuration;
        _supabaseClient = supabaseClient;
    }

    [Function("greeting")]
    public async Task<IActionResult> GreetUser([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        _ = await _supabaseClient.InitializeAsync();
        //var url = _supabaseClient.Postgrest.BaseUrl;
        ClaimsPrincipal user = req.HttpContext.User;

        return new OkObjectResult($"Welcome {user?.Identity?.Name}");
    }

    [Function("openai")]
    public static async Task<IActionResult> TestOpenAi(
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

    [Function("mcp")]
    public async Task<IActionResult> TestMcp(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            // Validate API key from header
            if (!ValidateApiKey(req, out var errorResponse))
            {
                return errorResponse;
            }

            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            var mcpRequest = string.IsNullOrEmpty(requestBody) 
                ? new JsonElement() 
                : JsonSerializer.Deserialize<JsonElement>(requestBody, _jsonSerializerOptions);

            // Get Supabase connection status
            await _supabaseClient.InitializeAsync();
            var supabaseUrl = _supabaseClient.Postgrest.BaseUrl;

            // Build health check response
            var healthData = new
            {
                success = true,
                authenticated = true,
                service = "Azure Functions MCP Endpoint",
                supabase = new
                {
                    connected = true,
                    url = supabaseUrl
                },
                timestamp = DateTime.UtcNow,
                message = "✅ MCP endpoint is healthy and authenticated"
            };

            return new OkObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(healthData, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mcp-health-check function");
            return new OkObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = false,
                            error = ex.Message
                        })
                    }
                },
                isError = true
            });
        }
    }

    private bool ValidateApiKey(HttpRequest req, out IActionResult errorResponse)
    {
        errorResponse = null!;
        
        // Get API key from environment
        var validApiKey = _configuration["MCP_API_KEY"] ?? Environment.GetEnvironmentVariable("MCP_API_KEY");
        
        if (string.IsNullOrEmpty(validApiKey))
        {
            _logger.LogWarning("MCP_API_KEY not configured");
            errorResponse = new OkObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = false,
                            error = "API key authentication not configured on server"
                        })
                    }
                },
                isError = true
            });
            return false;
        }

        // Check for API key in headers (X-API-Key or Authorization: Bearer)
        var apiKey = req.Headers["X-API-Key"].ToString();
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("API key missing from request");
            errorResponse = new UnauthorizedObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = false,
                            error = "API key required. Provide via X-API-Key header or Authorization: Bearer header"
                        })
                    }
                },
                isError = true
            });
            return false;
        }

        // Validate API key
        if (apiKey != validApiKey)
        {
            _logger.LogWarning("Invalid API key provided");
            errorResponse = new UnauthorizedObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = false,
                            error = "Invalid API key"
                        })
                    }
                },
                isError = true
            });
            return false;
        }

        return true;
    }
}
