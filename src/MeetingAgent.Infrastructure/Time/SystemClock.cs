using MeetingAgent.Application.Ports;

namespace MeetingAgent.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
