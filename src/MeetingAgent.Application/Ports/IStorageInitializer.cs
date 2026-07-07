namespace MeetingAgent.Application.Ports;

public interface IStorageInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
