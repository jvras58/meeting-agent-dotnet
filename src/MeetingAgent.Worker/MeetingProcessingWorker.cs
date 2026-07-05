namespace MeetingAgent.Worker;

public sealed class MeetingProcessingWorker : BackgroundService
{
    private readonly ILogger<MeetingProcessingWorker> _logger;

    public MeetingProcessingWorker(ILogger<MeetingProcessingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MeetingAgent.Worker background loop started. Queue consumer is still a placeholder for now.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("MeetingAgent.Worker heartbeat. Waiting for a real queue adapter implementation.");
            // TODO: Consumir eventos de RabbitMQ/Azure Service Bus e chamar ProcessMeetingUseCase.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
