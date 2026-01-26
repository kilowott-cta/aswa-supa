using System.Net.Http.Json;
using DomainBasic.Models.Dto;

namespace BlazorBasic.Services;

public class ChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _chatEndpoint;
    private SupabaseAuthenticationService _authService;

    public ChatService(IHttpClientFactory httpClientFactory, 
        SupabaseAuthenticationService authService)
    {
        _httpClientFactory = httpClientFactory;
        _chatEndpoint = "/api/chats";
        _authService = authService;
    }

    public async Task<ChatResponse?> SendMessageAsync(string message, string? conversationId = null)
    {
        var request = new ChatRequest
        {
            Query = message,
            ConversationId = conversationId
        };

        try
        {
            HttpClient? client = _httpClientFactory.CreateClient(name: "ApiClient");
            client.DefaultRequestHeaders.Add("apikey", _authService.SupabaseKey);
            client.DefaultRequestHeaders.Add("x-Authorization", _authService.AccessToken);
            var response = await client.PostAsJsonAsync(_chatEndpoint, request);
            
            // Handle 429 (Rate Limit) with retry-after
            if ((int)response.StatusCode == 429)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                await Task.Delay(retryAfter);
                response = await client.PostAsJsonAsync(_chatEndpoint, request);
            }
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatResponse>();
        }
        catch (HttpRequestException ex)
        {
            // Log diagnostic information
            Console.WriteLine($"Chat request failed: {ex.Message}");
            throw;
        }
    }
}
