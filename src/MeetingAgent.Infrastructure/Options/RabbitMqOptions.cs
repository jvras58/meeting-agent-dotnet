namespace MeetingAgent.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "meeting.processing.requested";
    public string DeadLetterQueueName { get; set; } = "meeting.processing.dead-letter";
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int OutboxBatchSize { get; set; } = 10;
}
