using System.Collections.Concurrent;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Meetings;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class InMemoryMeetingRepository : IMeetingRepository
{
    private readonly ConcurrentDictionary<Guid, Meeting> _store = new();

    public Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _store[meeting.Id] = meeting;
        return Task.CompletedTask;
    }

    public Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var meeting);
        return Task.FromResult(meeting);
    }

    public Task<IReadOnlyCollection<Meeting>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Meeting>>(_store.Values.OrderByDescending(m => m.CreatedAt).ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
