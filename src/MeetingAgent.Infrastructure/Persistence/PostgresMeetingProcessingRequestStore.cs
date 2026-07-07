using System.Text.Json;
using MeetingAgent.Application.Ports;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;
using Npgsql;
using NpgsqlTypes;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresMeetingProcessingRequestStore : IMeetingProcessingRequestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PostgresConnectionFactory _connectionFactory;

    public PostgresMeetingProcessingRequestStore(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveImportedMeetingAndQueueAsync(
        Meeting meeting,
        Transcript transcript,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpsertMeetingAsync(connection, transaction, meeting, cancellationToken);
        await UpsertTranscriptAsync(connection, transaction, transcript, cancellationToken);
        await InsertOutboxAsync(connection, transaction, message, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> QueueExistingMeetingAsync(
        Guid meetingId,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var updateMeeting = connection.CreateCommand();
        updateMeeting.Transaction = transaction;
        updateMeeting.CommandText = """
        UPDATE meetings
        SET status = @status,
            failure_reason = NULL,
            updated_at = @updated_at
        WHERE id = @id
        """;
        updateMeeting.Parameters.AddWithValue("id", meetingId);
        updateMeeting.Parameters.AddWithValue("status", (int)MeetingStatus.Queued);
        updateMeeting.Parameters.AddWithValue("updated_at", DateTimeOffset.UtcNow);

        var affectedRows = await updateMeeting.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await InsertOutboxAsync(connection, transaction, message, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task UpsertMeetingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Meeting meeting,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
        INSERT INTO meetings (
            id, title, external_meeting_id, online_meeting_id, join_web_url, organizer_email,
            start_time, end_time, status, failure_reason, created_at, updated_at
        )
        VALUES (
            @id, @title, @external_meeting_id, @online_meeting_id, @join_web_url, @organizer_email,
            @start_time, @end_time, @status, @failure_reason, @created_at, @updated_at
        )
        ON CONFLICT (id) DO UPDATE SET
            title = EXCLUDED.title,
            external_meeting_id = EXCLUDED.external_meeting_id,
            online_meeting_id = EXCLUDED.online_meeting_id,
            join_web_url = EXCLUDED.join_web_url,
            organizer_email = EXCLUDED.organizer_email,
            start_time = EXCLUDED.start_time,
            end_time = EXCLUDED.end_time,
            status = EXCLUDED.status,
            failure_reason = EXCLUDED.failure_reason,
            updated_at = EXCLUDED.updated_at
        """;

        command.Parameters.AddWithValue("id", meeting.Id);
        command.Parameters.AddWithValue("title", meeting.Title);
        AddNullableText(command, "external_meeting_id", meeting.ExternalMeetingId);
        AddNullableText(command, "online_meeting_id", meeting.OnlineMeetingId);
        AddNullableText(command, "join_web_url", meeting.JoinWebUrl);
        AddNullableText(command, "organizer_email", meeting.OrganizerEmail);
        AddNullableTimestamp(command, "start_time", meeting.StartTime);
        AddNullableTimestamp(command, "end_time", meeting.EndTime);
        command.Parameters.AddWithValue("status", (int)meeting.Status);
        AddNullableText(command, "failure_reason", meeting.FailureReason);
        command.Parameters.AddWithValue("created_at", meeting.CreatedAt.ToUniversalTime());
        command.Parameters.AddWithValue("updated_at", meeting.UpdatedAt.ToUniversalTime());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertTranscriptAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Transcript transcript,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
        INSERT INTO transcripts (id, meeting_id, source, language, raw_content, status, segments_json, created_at, updated_at)
        VALUES (@id, @meeting_id, @source, @language, @raw_content, @status, '[]'::jsonb, @created_at, @updated_at)
        ON CONFLICT (meeting_id) DO UPDATE SET
            source = EXCLUDED.source,
            language = EXCLUDED.language,
            raw_content = EXCLUDED.raw_content,
            status = EXCLUDED.status,
            updated_at = EXCLUDED.updated_at
        """;

        command.Parameters.AddWithValue("id", transcript.Id);
        command.Parameters.AddWithValue("meeting_id", transcript.MeetingId);
        command.Parameters.AddWithValue("source", transcript.Source);
        command.Parameters.AddWithValue("language", transcript.Language);
        command.Parameters.AddWithValue("raw_content", transcript.RawContent);
        command.Parameters.AddWithValue("status", (int)transcript.Status);
        command.Parameters.AddWithValue("created_at", transcript.CreatedAt.ToUniversalTime());
        command.Parameters.AddWithValue("updated_at", transcript.UpdatedAt.ToUniversalTime());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOutboxAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MeetingProcessingRequested message,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
        INSERT INTO outbox_messages (
            id, type, payload, status, attempts, available_at, created_at, updated_at
        )
        VALUES (
            @id, @type, CAST(@payload AS jsonb), 'pending', 0, @available_at, @created_at, @updated_at
        )
        """;

        var now = DateTimeOffset.UtcNow;
        var payload = message with { RequestedAt = message.RequestedAt ?? now };

        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("type", nameof(MeetingProcessingRequested));
        command.Parameters.AddWithValue("payload", JsonSerializer.Serialize(payload, JsonOptions));
        command.Parameters.AddWithValue("available_at", now);
        command.Parameters.AddWithValue("created_at", now);
        command.Parameters.AddWithValue("updated_at", now);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddNullableText(NpgsqlCommand command, string name, string? value)
    {
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text)
        {
            Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value
        });
    }

    private static void AddNullableTimestamp(NpgsqlCommand command, string name, DateTimeOffset? value)
    {
        command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
        {
            Value = value.HasValue ? value.Value.ToUniversalTime() : DBNull.Value
        });
    }
}
