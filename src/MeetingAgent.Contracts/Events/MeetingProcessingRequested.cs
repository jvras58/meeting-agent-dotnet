namespace MeetingAgent.Contracts.Events;

public sealed record MeetingProcessingRequested(
    Guid MeetingId,
    string SourceFormat = "text",
    DateTimeOffset? RequestedAt = null);
