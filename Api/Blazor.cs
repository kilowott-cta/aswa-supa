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
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public Blazor(ILogger<Blazor> logger, IConfiguration configuration, Supabase.Client supabaseClient, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _supabaseClient = supabaseClient;
        _httpClient = httpClientFactory.CreateClient("OpenAI");
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

    [Function("chat")]
    public async Task<IActionResult> Chat([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            var token = req.Headers["x-Authorization"].ToString().Replace("Bearer ", "");
            var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());
            
            if (session.User == null)
            {
                return new UnauthorizedResult();
            }

            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<DomainBasic.Models.Dto.ChatRequest>(requestBody, _jsonSerializerOptions);

            if (string.IsNullOrWhiteSpace(chatRequest?.Query))
            {
                return new BadRequestObjectResult(new DomainBasic.Models.Dto.ChatResponse 
                { 
                    Success = false, 
                    Error = "Query cannot be empty" 
                });
            }

            var openAiKey = _configuration["OPENAI_API_KEY"];
            if (string.IsNullOrEmpty(openAiKey))
            {
                return new BadRequestObjectResult(new DomainBasic.Models.Dto.ChatResponse 
                { 
                    Success = false, 
                    Error = "OpenAI API key not configured" 
                });
            }

            var model = _configuration["OPENAI_MODEL"] ?? "gpt-4o-mini";
            var messages = new[] 
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = chatRequest.Query }
                
            }.ToList();

            // Add conversation history
            if (chatRequest.ConversationHistory != null)
            {
                foreach (var msg in chatRequest.ConversationHistory)
                {
                    messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
                    _logger.LogInformation($"Added message to history: {msg.Role} - {msg.Content}"); 
                }
            }
            
            // Build OpenAI API request
            var openAIRequest = new
            {
                model,
                max_tokens = 500,
                temperature = 0.7,  
                user = chatRequest.ConversationId, // Track usage per conversation
                messages 
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, 
                "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(openAIRequest), 
                    System.Text.Encoding.UTF8, 
                    "application/json")
            };
            requestMessage.Headers.Add("Authorization", $"Bearer {openAiKey}");

            var response = await _httpClient.SendAsync(requestMessage);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API error: {errorContent}");
                return new BadRequestObjectResult(new DomainBasic.Models.Dto.ChatResponse 
                { 
                    Success = false, 
                    Error = $"OpenAI API error: {response.StatusCode}" 
                });
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var openAiResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            var assistantResponse = openAiResponse
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            // Build updated conversation history
            var updatedHistory = chatRequest.ConversationHistory ?? new List<DomainBasic.Models.Dto.ChatMessage>();
            updatedHistory.Add(new DomainBasic.Models.Dto.ChatMessage { Role = "user", Content = chatRequest.Query });
            updatedHistory.Add(new DomainBasic.Models.Dto.ChatMessage { Role = "assistant", Content = assistantResponse });

            return new OkObjectResult(new DomainBasic.Models.Dto.ChatResponse
            {
                Message = assistantResponse,
                ConversationHistory = updatedHistory,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat function");
            return new BadRequestObjectResult(new DomainBasic.Models.Dto.ChatResponse 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
    }
}