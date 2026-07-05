namespace MeetingAgent.Application.Ports;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
