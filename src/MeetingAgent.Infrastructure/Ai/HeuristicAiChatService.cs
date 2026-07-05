using MeetingAgent.Application.Ports;

namespace MeetingAgent.Infrastructure.Ai;

public sealed class HeuristicAiChatService : IAiChatService
{
    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var response = "IA heurística ativa. Configure AI_PROVIDER=ollama para usar um modelo local OpenAI-compatible.";
        return Task.FromResult(response);
    }
}
