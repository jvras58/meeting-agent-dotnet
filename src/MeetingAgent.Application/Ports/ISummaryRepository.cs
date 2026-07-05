using MeetingAgent.Domain.Summaries;

namespace MeetingAgent.Application.Ports;

public interface ISummaryRepository
{
    Task AddOrReplaceAsync(MeetingSummary summary, CancellationToken cancellationToken = default);
    Task<MeetingSummary?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
