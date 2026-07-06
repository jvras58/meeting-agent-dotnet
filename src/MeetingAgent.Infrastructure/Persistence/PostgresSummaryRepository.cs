using System.Text.Json;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Summaries;
using Npgsql;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresSummaryRepository : ISummaryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly Dictionary<Guid, MeetingSummary> _tracked = [];

    public PostgresSummaryRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddOrReplaceAsync(MeetingSummary summary, CancellationToken cancellationToken = default)
    {
        _tracked[summary.MeetingId] = summary;
        return Task.CompletedTask;
    }

    public async Task<MeetingSummary?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        SELECT id, meeting_id, executive_summary, markdown, main_points_json::text, decisions_json::text,
               action_items_json::text, risks_json::text, open_questions_json::text, created_at, updated_at
        FROM summaries
        WHERE meeting_id = @meeting_id
        """;
        command.Parameters.AddWithValue("meeting_id", meetingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return MeetingSummary.Restore(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            Deserialize<List<string>>(reader.GetString(4)),
            Deserialize<List<Decision>>(reader.GetString(5)),
            Deserialize<List<ActionItem>>(reader.GetString(6)),
            Deserialize<List<Risk>>(reader.GetString(7)),
            Deserialize<List<string>>(reader.GetString(8)),
            reader.GetFieldValue<DateTimeOffset>(9),
            reader.GetFieldValue<DateTimeOffset>(10));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tracked.Count == 0) return;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        foreach (var summary in _tracked.Values)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
            INSERT INTO summaries (
                id, meeting_id, executive_summary, markdown, main_points_json, decisions_json,
                action_items_json, risks_json, open_questions_json, created_at, updated_at
            )
            VALUES (
                @id, @meeting_id, @executive_summary, @markdown, CAST(@main_points_json AS jsonb), CAST(@decisions_json AS jsonb),
                CAST(@action_items_json AS jsonb), CAST(@risks_json AS jsonb), CAST(@open_questions_json AS jsonb), @created_at, @updated_at
            )
            ON CONFLICT (meeting_id) DO UPDATE SET
                executive_summary = EXCLUDED.executive_summary,
                markdown = EXCLUDED.markdown,
                main_points_json = EXCLUDED.main_points_json,
                decisions_json = EXCLUDED.decisions_json,
                action_items_json = EXCLUDED.action_items_json,
                risks_json = EXCLUDED.risks_json,
                open_questions_json = EXCLUDED.open_questions_json,
                updated_at = EXCLUDED.updated_at
            """;

            command.Parameters.AddWithValue("id", summary.Id);
            command.Parameters.AddWithValue("meeting_id", summary.MeetingId);
            command.Parameters.AddWithValue("executive_summary", summary.ExecutiveSummary);
            command.Parameters.AddWithValue("markdown", summary.Markdown);
            command.Parameters.AddWithValue("main_points_json", Serialize(summary.MainPoints));
            command.Parameters.AddWithValue("decisions_json", Serialize(summary.Decisions));
            command.Parameters.AddWithValue("action_items_json", Serialize(summary.ActionItems));
            command.Parameters.AddWithValue("risks_json", Serialize(summary.Risks));
            command.Parameters.AddWithValue("open_questions_json", Serialize(summary.OpenQuestions));
            command.Parameters.AddWithValue("created_at", summary.CreatedAt);
            command.Parameters.AddWithValue("updated_at", summary.UpdatedAt);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _tracked.Clear();
    }

    private static string Serialize<T>(IEnumerable<T> values) => JsonSerializer.Serialize(values, JsonOptions);

    private static T Deserialize<T>(string json) where T : new()
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }
}
