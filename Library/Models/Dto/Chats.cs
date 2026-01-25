namespace DomainBasic.Models.Dto;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty; // Partition key for user isolation
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatRequest
{
    public string? ConversationId { get; set; }
    public string Query { get; set; } = string.Empty;
    public List<ChatMessage> ConversationHistory { get; set; } = new();
}

public class ChatResponse
{
    public string? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ChatMessage> ConversationHistory { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
}
