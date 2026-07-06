using MeetingAgent.Contracts.Events;

namespace MeetingAgent.Application.Ports;

public interface IMeetingProcessingJobPublisher
{
    Task PublishAsync(MeetingProcessingRequested message, CancellationToken cancellationToken = default);
}
