using MeetingAgent.Contracts.Events;

namespace MeetingAgent.Application.Ports;

public sealed record OutboxMeetingProcessingMessage(
    Guid Id,
    MeetingProcessingRequested Payload,
    int Attempts);

public interface IMeetingProcessingOutboxRepository
{
    Task<IReadOnlyCollection<OutboxMeetingProcessingMessage>> GetPendingAsync(
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid outboxMessageId, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid outboxMessageId,
        string error,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default);
}
