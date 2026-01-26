using System.Net.Http.Headers;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly string? _openAiKey;

    public Blazor(ILogger<Blazor> logger, IConfiguration configuration, Supabase.Client supabaseClient, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _supabaseClient = supabaseClient;
        _httpClientFactory = httpClientFactory;
        _openAiKey = _configuration["OPENAI_API_KEY"];
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

    [Function("upload-files")]
    public async Task<IActionResult> UploadFiles([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
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

    [Function("chats")]
    public async Task<IActionResult> Chats([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
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

            if (string.IsNullOrEmpty(_openAiKey))
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

            var client = _httpClientFactory.CreateClient("OpenAI");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, 
                "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(openAIRequest), 
                    System.Text.Encoding.UTF8, 
                    "application/json")
            };
            requestMessage.Headers.Add("Authorization", $"Bearer {_openAiKey}");

            var response = await client.SendAsync(requestMessage);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API error: {errorContent}");
                return new BadRequestObjectResult(new DomainBasic.Models.Dto.ChatResponse 
                { 
                    Success = false, 
                    Error = $"OpenAI API error: {response.StatusCode} : {response.ReasonPhrase}" 
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

    [Function("mcp-query-projects")]
    public async Task<IActionResult> McpQueryProjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            using var reader = new StreamReader(req.Body);
            var requestBody = await reader.ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<JsonElement>(requestBody, _jsonSerializerOptions);

            var token = req.Headers["x-Authorization"].ToString().Replace("Bearer ", "");
            var session = await _supabaseClient.Auth.SetSession(token, Guid.NewGuid().ToString());

            // Extract parameters from MCP request format
            string? filterBy = null;
            string? filterValue = null;
            int? limit = null;

            if (mcpRequest.TryGetProperty("params", out var params_))
            {
                if (params_.TryGetProperty("filterBy", out var filterByProp))
                    filterBy = filterByProp.GetString();
                if (params_.TryGetProperty("filterValue", out var filterValueProp))
                    filterValue = filterValueProp.GetString();
                if (params_.TryGetProperty("limit", out var limitProp))
                    limit = limitProp.GetInt32();
            }
            
            // Query projects
            var query = _supabaseClient.From<DomainBasic.Models.Dbo.Project>();

            // Apply filters if provided
            if (!string.IsNullOrEmpty(filterBy) && !string.IsNullOrEmpty(filterValue))
            {
                switch (filterBy.ToLower())
                {
                    case "status":
                        query.Filter("status", Supabase.Postgrest.Constants.Operator.Equals, filterValue);
                        break;
                    case "client":
                        query.Filter("client_name", Supabase.Postgrest.Constants.Operator.Equals, filterValue);
                        break;
                    case "stage":
                        query.Filter("stage", Supabase.Postgrest.Constants.Operator.Equals, filterValue);
                        break;
                }
            }

            // Apply limit if provided
            if (limit.HasValue && limit.Value > 0)
            {
                query.Limit(limit.Value);
            }

            var results = await query.Get();
            var projects = results.Models.Select(p => p.ToDtoFromDbo()).ToList();

            // Return in MCP format
            return new OkObjectResult(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(new
                        {
                            success = true,
                            count = projects.Count,
                            projects
                        }, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mcp-query-projects function");
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
}