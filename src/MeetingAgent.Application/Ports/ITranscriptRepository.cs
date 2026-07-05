using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.Ports;

public interface ITranscriptRepository
{
    Task AddAsync(Transcript transcript, CancellationToken cancellationToken = default);
    Task<Transcript?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
