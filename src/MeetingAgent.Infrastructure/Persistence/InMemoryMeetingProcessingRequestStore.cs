using MeetingAgent.Application.Ports;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class InMemoryMeetingProcessingRequestStore : IMeetingProcessingRequestStore
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly IMeetingProcessingJobPublisher _jobPublisher;

    public InMemoryMeetingProcessingRequestStore(
        IMeetingRepository meetingRepository,
        ITranscriptRepository transcriptRepository,
        IMeetingProcessingJobPublisher jobPublisher)
    {
        _meetingRepository = meetingRepository;
        _transcriptRepository = transcriptRepository;
        _jobPublisher = jobPublisher;
    }

    public async Task SaveImportedMeetingAndQueueAsync(
        Meeting meeting,
        Transcript transcript,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default)
    {
        await _meetingRepository.AddAsync(meeting, cancellationToken);
        await _transcriptRepository.AddAsync(transcript, cancellationToken);
        await _meetingRepository.SaveChangesAsync(cancellationToken);
        await _transcriptRepository.SaveChangesAsync(cancellationToken);
        await _jobPublisher.PublishAsync(message, cancellationToken);
    }

    public async Task<bool> QueueExistingMeetingAsync(
        Guid meetingId,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId, cancellationToken);
        if (meeting is null) return false;

        meeting.MarkQueued();
        await _meetingRepository.SaveChangesAsync(cancellationToken);
        await _jobPublisher.PublishAsync(message, cancellationToken);
        return true;
    }
}
