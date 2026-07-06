namespace MeetingAgent.Domain.Meetings;

public enum MeetingStatus
{
    Created = 0,
    TranscriptImported = 1,
    Queued = 2,
    Processing = 3,
    SummaryGenerated = 4,
    Failed = 5
}
