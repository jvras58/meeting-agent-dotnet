namespace MeetingAgent.Contracts.Events;

public sealed record MeetingTranscriptAvailable(
    Guid EventId,
    Guid MeetingId,
    string? ExternalMeetingId,
    string? TranscriptId,
    DateTimeOffset ReceivedAt,
    string CorrelationId);
