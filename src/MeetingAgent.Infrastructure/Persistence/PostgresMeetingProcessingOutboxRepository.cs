using System.Text.Json;
using MeetingAgent.Application.Ports;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Infrastructure.Options;
using Npgsql;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresMeetingProcessingOutboxRepository : IMeetingProcessingOutboxRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgresConnectionFactory _connectionFactory;

    public PostgresMeetingProcessingOutboxRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<OutboxMeetingProcessingMessage>> GetPendingAsync(
        int batchSize,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, payload::text, attempts
        FROM outbox_messages
        WHERE type = @type
          AND status IN ('pending', 'failed')
          AND attempts < @max_attempts
          AND available_at <= @now
        ORDER BY created_at
        LIMIT @batch_size
        """;
        command.Parameters.AddWithValue("type", nameof(MeetingProcessingRequested));
        command.Parameters.AddWithValue("max_attempts", maxAttempts);
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("batch_size", batchSize);

        var messages = new List<OutboxMeetingProcessingMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var payload = JsonSerializer.Deserialize<MeetingProcessingRequested>(reader.GetString(1), JsonOptions);
            if (payload is null || payload.MeetingId == Guid.Empty)
            {
                continue;
            }

            messages.Add(new OutboxMeetingProcessingMessage(
                reader.GetGuid(0),
                payload,
                reader.GetInt32(2)));
        }

        return messages;
    }

    public async Task MarkPublishedAsync(Guid outboxMessageId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        UPDATE outbox_messages
        SET status = 'published',
            published_at = @now,
            updated_at = @now,
            error = NULL
        WHERE id = @id
        """;
        command.Parameters.AddWithValue("id", outboxMessageId);
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid outboxMessageId,
        string error,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        UPDATE outbox_messages
        SET attempts = attempts + 1,
            status = CASE WHEN attempts + 1 >= @max_attempts THEN 'dead_letter' ELSE 'failed' END,
            error = @error,
            available_at = @available_at,
            updated_at = @now
        WHERE id = @id
        """;
        var now = DateTimeOffset.UtcNow;
        command.Parameters.AddWithValue("id", outboxMessageId);
        command.Parameters.AddWithValue("max_attempts", maxAttempts);
        command.Parameters.AddWithValue("error", TrimError(error));
        command.Parameters.AddWithValue("available_at", now.Add(retryDelay));
        command.Parameters.AddWithValue("now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string TrimError(string error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "Unknown error";
        return error.Length <= 2000 ? error : error[..2000];
    }
}
