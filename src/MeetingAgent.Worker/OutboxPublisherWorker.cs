using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingAgent.Worker;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox publisher started. BatchSize={BatchSize}, MaxAttempts={MaxAttempts}, RetryDelaySeconds={RetryDelaySeconds}.",
            _options.OutboxBatchSize,
            _options.MaxRetryAttempts,
            _options.RetryDelaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox publisher loop failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IMeetingProcessingOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IMeetingProcessingJobPublisher>();

        var messages = await outbox.GetPendingAsync(
            _options.OutboxBatchSize,
            _options.MaxRetryAttempts,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Payload, cancellationToken);
                await outbox.MarkPublishedAsync(message.Id, cancellationToken);

                _logger.LogInformation(
                    "Outbox message published. OutboxMessageId={OutboxMessageId}, MeetingId={MeetingId}.",
                    message.Id,
                    message.Payload.MeetingId);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await outbox.MarkFailedAsync(
                    message.Id,
                    exception.Message,
                    _options.MaxRetryAttempts,
                    TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Outbox message failed. OutboxMessageId={OutboxMessageId}, Attempts={Attempts}.",
                    message.Id,
                    message.Attempts + 1);
            }
        }
    }
}
