using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NvidiaChatBot.Models;

namespace NvidiaChatBot.Services;

public class NvidiaAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public NvidiaAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://integrate.api.nvidia.com/v1/");
        _apiKey = configuration["NvidiaApiKey"] ?? "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";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async IAsyncEnumerable<AiStreamEvent> StreamChatAsync(List<ChatMessage> history, ChatMessage newMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
        messages.Add(new { role = newMessage.Role, content = newMessage.Content });

        var requestBody = new
        {
            model = "nvidia/nemotron-3-ultra-550b-a55b",
            messages = messages,
            temperature = 1.0,
            top_p = 0.95,
            max_tokens = 16384,
            stream = true,
            chat_template_kwargs = new
            {
                enable_thinking = true
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Details: {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                JsonDocument? doc = null;
                try
                {
                    doc = JsonDocument.Parse(data);
                }
                catch
                {
                    continue;
                }

                if (doc != null)
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        
                        string? contentChunk = delta.TryGetProperty("content", out var cProp) ? cProp.GetString() : null;
                        string? reasoningChunk = delta.TryGetProperty("reasoning_content", out var rProp) ? rProp.GetString() : null;

                        if (!string.IsNullOrEmpty(contentChunk) || !string.IsNullOrEmpty(reasoningChunk))
                        {
                            yield return new AiStreamEvent
                            {
                                ContentChunk = contentChunk,
                                ReasoningChunk = reasoningChunk
                            };
                        }
                    }
                }
            }
        }
    }
}

public class AiStreamEvent
{
    public string? ContentChunk { get; set; }
    public string? ReasoningChunk { get; set; }
}
