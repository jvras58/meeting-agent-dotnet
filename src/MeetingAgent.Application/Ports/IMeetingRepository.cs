using MeetingAgent.Domain.Meetings;

namespace MeetingAgent.Application.Ports;

public interface IMeetingRepository
{
    Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default);
    Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Meeting>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
