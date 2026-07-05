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
        _logger.LogInformation("MeetingAgent.Worker started. Configure a real queue adapter before production use.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Consumir eventos de RabbitMQ/Azure Service Bus e chamar ProcessMeetingUseCase.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
