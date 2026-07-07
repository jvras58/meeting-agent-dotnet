using System.Text;
using System.Text.Json;
using MeetingAgent.Application.UseCases;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingAgent.Worker;

public sealed class MeetingProcessingWorker : BackgroundService
{
    private const string RetryHeader = "x-retry-count";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MeetingProcessingWorker> _logger;
    private readonly RabbitMqOptions _options;

    public MeetingProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<MeetingProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MeetingAgent.Worker RabbitMQ loop started. Host={RabbitHost}, Queue={QueueName}, DeadLetterQueue={DeadLetterQueueName}.",
            _options.Host,
            _options.QueueName,
            _options.DeadLetterQueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Worker loop failed. Retrying RabbitMQ connection in 5 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        DeclareQueues(channel);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("Worker connected to RabbitMQ and is waiting for jobs.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = channel.BasicGet(_options.QueueName, autoAck: false);
            if (result is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            MeetingProcessingRequested? message = null;

            try
            {
                var json = Encoding.UTF8.GetString(result.Body.ToArray());
                message = JsonSerializer.Deserialize<MeetingProcessingRequested>(json, JsonOptions);

                if (message is null || message.MeetingId == Guid.Empty)
                {
                    _logger.LogWarning("Discarding invalid meeting processing job.");
                    channel.BasicAck(result.DeliveryTag, multiple: false);
                    continue;
                }

                _logger.LogInformation(
                    "Processing meeting job. MeetingId={MeetingId}, SourceFormat={SourceFormat}.",
                    message.MeetingId,
                    message.SourceFormat);

                using var scope = _scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessMeetingUseCase>();
                await useCase.ExecuteAsync(message.MeetingId, message.SourceFormat, stoppingToken);

                channel.BasicAck(result.DeliveryTag, multiple: false);

                _logger.LogInformation(
                    "Meeting job processed. MeetingId={MeetingId}.",
                    message.MeetingId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
                throw;
            }
            catch (Exception exception)
            {
                HandleJobFailure(channel, result, message, exception);
                await Task.Delay(TimeSpan.FromSeconds(_options.RetryDelaySeconds), stoppingToken);
            }
        }
    }

    private void DeclareQueues(IModel channel)
    {
        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: _options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    private void HandleJobFailure(
        IModel channel,
        BasicGetResult result,
        MeetingProcessingRequested? message,
        Exception exception)
    {
        var retryCount = GetRetryCount(result.BasicProperties) + 1;
        var shouldDeadLetter = retryCount >= _options.MaxRetryAttempts;
        var targetQueue = shouldDeadLetter ? _options.DeadLetterQueueName : _options.QueueName;

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = result.BasicProperties?.ContentType ?? "application/json";
        properties.Type = result.BasicProperties?.Type ?? nameof(MeetingProcessingRequested);
        properties.MessageId = result.BasicProperties?.MessageId ?? message?.MeetingId.ToString();
        properties.Headers = new Dictionary<string, object>
        {
            [RetryHeader] = retryCount,
            ["x-error"] = TrimError(exception.Message)
        };

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: targetQueue,
            basicProperties: properties,
            body: result.Body);

        channel.BasicAck(result.DeliveryTag, multiple: false);

        if (shouldDeadLetter)
        {
            _logger.LogError(
                exception,
                "Meeting job moved to dead-letter queue. MeetingId={MeetingId}, Attempts={Attempts}, DeadLetterQueue={DeadLetterQueueName}.",
                message?.MeetingId,
                retryCount,
                _options.DeadLetterQueueName);
            return;
        }

        _logger.LogWarning(
            exception,
            "Meeting job failed and was requeued with bounded retry. MeetingId={MeetingId}, Attempt={Attempt}, MaxAttempts={MaxAttempts}.",
            message?.MeetingId,
            retryCount,
            _options.MaxRetryAttempts);
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers is null) return 0;
        if (!properties.Headers.TryGetValue(RetryHeader, out var value)) return 0;

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            int retryCount => retryCount,
            long retryCount => (int)retryCount,
            _ => 0
        };
    }

    private static string TrimError(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "Unknown error";
        return error.Length <= 1000 ? error : error[..1000];
    }
}
