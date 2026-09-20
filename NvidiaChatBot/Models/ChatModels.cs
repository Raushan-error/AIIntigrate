using System.Text.Json.Serialization;

namespace NvidiaChatBot.Models;

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    // We will support a list of contents (e.g., text, image_url)
    [JsonPropertyName("content")]
    public object Content { get; set; } = string.Empty;

    // To display in UI
    [JsonIgnore]
    public string DisplayText { get; set; } = string.Empty;

    [JsonIgnore]
    public string ReasoningText { get; set; } = string.Empty;
}

public class ChatSession
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime LastActive { get; set; } = DateTime.UtcNow;
    
    // Maintain a list of all messages in the session
    public List<ChatMessage> Messages { get; set; } = new();
}
