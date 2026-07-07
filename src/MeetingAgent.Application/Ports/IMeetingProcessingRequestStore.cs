using MeetingAgent.Contracts.Events;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.Ports;

public interface IMeetingProcessingRequestStore
{
    Task SaveImportedMeetingAndQueueAsync(
        Meeting meeting,
        Transcript transcript,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default);

    Task<bool> QueueExistingMeetingAsync(
        Guid meetingId,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default);
}
