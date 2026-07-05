using System.Collections.Concurrent;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class InMemoryTranscriptRepository : ITranscriptRepository
{
    private readonly ConcurrentDictionary<Guid, Transcript> _byMeetingId = new();

    public Task AddAsync(Transcript transcript, CancellationToken cancellationToken = default)
    {
        _byMeetingId[transcript.MeetingId] = transcript;
        return Task.CompletedTask;
    }

    public Task<Transcript?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        _byMeetingId.TryGetValue(meetingId, out var transcript);
        return Task.FromResult(transcript);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
