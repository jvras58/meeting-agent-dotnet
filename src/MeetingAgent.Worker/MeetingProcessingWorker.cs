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
            "MeetingAgent.Worker RabbitMQ loop started. Host={RabbitHost}, Queue={QueueName}.",
            _options.Host,
            _options.QueueName);

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

        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

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
                if (message is not null)
                {
                    channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
                }

                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Meeting job failed. MeetingId={MeetingId}. Requeueing job.",
                    message?.MeetingId);

                channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
