using System.Collections.Concurrent;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Summaries;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class InMemorySummaryRepository : ISummaryRepository
{
    private readonly ConcurrentDictionary<Guid, MeetingSummary> _byMeetingId = new();

    public Task AddOrReplaceAsync(MeetingSummary summary, CancellationToken cancellationToken = default)
    {
        _byMeetingId[summary.MeetingId] = summary;
        return Task.CompletedTask;
    }

    public Task<MeetingSummary?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        _byMeetingId.TryGetValue(meetingId, out var summary);
        return Task.FromResult(summary);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
