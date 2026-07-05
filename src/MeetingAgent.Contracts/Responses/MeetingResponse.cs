namespace MeetingAgent.Contracts.Responses;

public sealed record MeetingResponse(
    Guid Id,
    string Title,
    string Status,
    string? OrganizerEmail,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
