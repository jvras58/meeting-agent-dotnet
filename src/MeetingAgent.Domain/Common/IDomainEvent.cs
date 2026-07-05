namespace MeetingAgent.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
