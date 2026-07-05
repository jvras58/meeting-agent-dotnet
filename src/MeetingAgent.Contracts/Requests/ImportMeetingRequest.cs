namespace MeetingAgent.Contracts.Requests;

public sealed record ImportMeetingRequest(
    string Title,
    string RawTranscript,
    string? Source = "manual",
    string? SourceFormat = "text",
    string? Language = "pt-BR",
    string? ExternalMeetingId = null,
    string? OnlineMeetingId = null,
    string? JoinWebUrl = null,
    string? OrganizerEmail = null,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null);
