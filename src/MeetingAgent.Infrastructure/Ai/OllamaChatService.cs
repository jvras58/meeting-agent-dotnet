using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingAgent.Infrastructure.Ai;

public sealed class OllamaChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;

    public OllamaChatService(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            false,
            [
                new OllamaMessage("system", systemPrompt),
                new OllamaMessage("user", userPrompt)
            ]);

        using var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        return result?.Message?.Content ?? string.Empty;
    }

    private sealed record OllamaChatRequest(string Model, bool Stream, IReadOnlyCollection<OllamaMessage> Messages);
    private sealed record OllamaMessage(string Role, string Content);
    private sealed record OllamaChatResponse([property: JsonPropertyName("message")] OllamaMessageResponse? Message);
    private sealed record OllamaMessageResponse([property: JsonPropertyName("content")] string Content);
}
