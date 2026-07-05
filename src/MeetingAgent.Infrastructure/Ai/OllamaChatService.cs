using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingAgent.Infrastructure.Ai;

public sealed class OllamaChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OllamaChatService> _logger;

    public OllamaChatService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OllamaChatService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
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

        _logger.LogInformation(
            "Calling Ollama. Model={Model}, BaseAddress={BaseAddress}, UserPromptLength={UserPromptLength}.",
            _options.Model,
            _httpClient.BaseAddress,
            userPrompt.Length);

        using var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        var content = result?.Message?.Content ?? string.Empty;

        _logger.LogInformation(
            "Ollama response received. Model={Model}, ResponseLength={ResponseLength}.",
            _options.Model,
            content.Length);

        return content;
    }

    private sealed record OllamaChatRequest(string Model, bool Stream, IReadOnlyCollection<OllamaMessage> Messages);
    private sealed record OllamaMessage(string Role, string Content);
    private sealed record OllamaChatResponse([property: JsonPropertyName("message")] OllamaMessageResponse? Message);
    private sealed record OllamaMessageResponse([property: JsonPropertyName("content")] string Content);
}
