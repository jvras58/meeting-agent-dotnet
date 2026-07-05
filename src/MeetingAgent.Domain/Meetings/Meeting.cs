using MeetingAgent.Domain.Common;

namespace MeetingAgent.Domain.Meetings;

public sealed class Meeting : Entity
{
    private Meeting(
        Guid id,
        string title,
        string? externalMeetingId,
        string? onlineMeetingId,
        string? joinWebUrl,
        string? organizerEmail,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime) : base(id)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Untitled meeting" : title.Trim();
        ExternalMeetingId = externalMeetingId;
        OnlineMeetingId = onlineMeetingId;
        JoinWebUrl = joinWebUrl;
        OrganizerEmail = organizerEmail;
        StartTime = startTime;
        EndTime = endTime;
        Status = MeetingStatus.Created;
    }

    public string Title { get; private set; }
    public string? ExternalMeetingId { get; private set; }
    public string? OnlineMeetingId { get; private set; }
    public string? JoinWebUrl { get; private set; }
    public string? OrganizerEmail { get; private set; }
    public DateTimeOffset? StartTime { get; private set; }
    public DateTimeOffset? EndTime { get; private set; }
    public MeetingStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public static Meeting Create(
        string title,
        string? externalMeetingId = null,
        string? onlineMeetingId = null,
        string? joinWebUrl = null,
        string? organizerEmail = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        return new Meeting(Guid.NewGuid(), title, externalMeetingId, onlineMeetingId, joinWebUrl, organizerEmail, startTime, endTime);
    }

    public void MarkTranscriptImported()
    {
        Status = MeetingStatus.TranscriptImported;
        FailureReason = null;
        Touch();
    }

    public void MarkProcessing()
    {
        Status = MeetingStatus.Processing;
        FailureReason = null;
        Touch();
    }

    public void MarkSummaryGenerated()
    {
        Status = MeetingStatus.SummaryGenerated;
        FailureReason = null;
        Touch();
    }

    public void MarkFailed(string reason)
    {
        Status = MeetingStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown failure" : reason.Trim();
        Touch();
    }
}
