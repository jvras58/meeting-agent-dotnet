namespace MeetingAgent.Domain.Meetings;

public enum MeetingStatus
{
    Created = 0,
    TranscriptImported = 1,
    Processing = 2,
    SummaryGenerated = 3,
    Failed = 4
}
