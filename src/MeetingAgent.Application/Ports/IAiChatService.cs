namespace MeetingAgent.Application.Ports;

public interface IAiChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
