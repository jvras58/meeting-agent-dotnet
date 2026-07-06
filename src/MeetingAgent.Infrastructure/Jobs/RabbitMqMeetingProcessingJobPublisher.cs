using System.Text;
using System.Text.Json;
using MeetingAgent.Application.Ports;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MeetingAgent.Infrastructure.Jobs;

public sealed class RabbitMqMeetingProcessingJobPublisher : IMeetingProcessingJobPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMeetingProcessingJobPublisher> _logger;

    public RabbitMqMeetingProcessingJobPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMeetingProcessingJobPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync(MeetingProcessingRequested message, CancellationToken cancellationToken = default)
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

        var payload = message with { RequestedAt = message.RequestedAt ?? DateTimeOffset.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = nameof(MeetingProcessingRequested);
        properties.MessageId = payload.MeetingId.ToString();

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published meeting processing job. MeetingId={MeetingId}, Queue={QueueName}.",
            payload.MeetingId,
            _options.QueueName);

        return Task.CompletedTask;
    }
}
