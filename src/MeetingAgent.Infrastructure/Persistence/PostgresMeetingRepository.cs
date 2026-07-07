using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Meetings;
using Npgsql;
using NpgsqlTypes;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresMeetingRepository : IMeetingRepository
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly Dictionary<Guid, Meeting> _tracked = [];

    public PostgresMeetingRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        _tracked[meeting.Id] = meeting;
        return Task.CompletedTask;
    }

    public async Task<Meeting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, title, external_meeting_id, online_meeting_id, join_web_url, organizer_email,
               start_time, end_time, status, failure_reason, created_at, updated_at
        FROM meetings
        WHERE id = @id
        """;
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var meeting = ReadMeeting(reader);
        _tracked[meeting.Id] = meeting;
        return meeting;
    }

    public async Task<IReadOnlyCollection<Meeting>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, title, external_meeting_id, online_meeting_id, join_web_url, organizer_email,
               start_time, end_time, status, failure_reason, created_at, updated_at
        FROM meetings
        ORDER BY created_at DESC
        LIMIT 100
        """;

        var meetings = new List<Meeting>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            meetings.Add(ReadMeeting(reader));
        }

        return meetings;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tracked.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        foreach (var meeting in _tracked.Values)
        {
            await using var command = connection.CreateCommand();
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

        _tracked.Clear();
    }

    private static Meeting ReadMeeting(NpgsqlDataReader reader)
    {
        return Meeting.Restore(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            (MeetingStatus)reader.GetInt32(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetFieldValue<DateTimeOffset>(10),
            reader.GetFieldValue<DateTimeOffset>(11));
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
