using System.Text.Json;
using MeetingAgent.Application.Ports;
using MeetingAgent.Domain.Transcripts;
using Npgsql;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresTranscriptRepository : ITranscriptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly Dictionary<Guid, Transcript> _tracked = [];

    public PostgresTranscriptRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(Transcript transcript, CancellationToken cancellationToken = default)
    {
        _tracked[transcript.MeetingId] = transcript;
        return Task.CompletedTask;
    }

    public async Task<Transcript?> GetByMeetingIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, meeting_id, source, language, raw_content, status, segments_json::text, created_at, updated_at FROM transcripts WHERE meeting_id = @meeting_id";
        command.Parameters.AddWithValue("meeting_id", meetingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var transcript = Transcript.Restore(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            (TranscriptStatus)reader.GetInt32(5),
            DeserializeSegments(reader.GetString(6)),
            reader.GetFieldValue<DateTimeOffset>(7),
            reader.GetFieldValue<DateTimeOffset>(8));

        _tracked[transcript.MeetingId] = transcript;
        return transcript;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tracked.Count == 0) return;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        foreach (var transcript in _tracked.Values)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
            INSERT INTO transcripts (id, meeting_id, source, language, raw_content, status, segments_json, created_at, updated_at)
            VALUES (@id, @meeting_id, @source, @language, @raw_content, @status, @segments_json::jsonb, @created_at, @updated_at)
            ON CONFLICT (meeting_id) DO UPDATE SET
                source = EXCLUDED.source,
                language = EXCLUDED.language,
                raw_content = EXCLUDED.raw_content,
                status = EXCLUDED.status,
                segments_json = EXCLUDED.segments_json,
                updated_at = EXCLUDED.updated_at
            """;

            command.Parameters.AddWithValue("id", transcript.Id);
            command.Parameters.AddWithValue("meeting_id", transcript.MeetingId);
            command.Parameters.AddWithValue("source", transcript.Source);
            command.Parameters.AddWithValue("language", transcript.Language);
            command.Parameters.AddWithValue("raw_content", transcript.RawContent);
            command.Parameters.AddWithValue("status", (int)transcript.Status);
            command.Parameters.AddWithValue("segments_json", SerializeSegments(transcript.Segments));
            command.Parameters.AddWithValue("created_at", transcript.CreatedAt);
            command.Parameters.AddWithValue("updated_at", transcript.UpdatedAt);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _tracked.Clear();
    }

    private static string SerializeSegments(IEnumerable<TranscriptSegment> segments)
    {
        var stored = segments.Select(segment => new StoredTranscriptSegment(segment.Id, segment.SpeakerName, segment.Start.Ticks, segment.End.Ticks, segment.Text, segment.CleanText));
        return JsonSerializer.Serialize(stored, JsonOptions);
    }

    private static IReadOnlyCollection<TranscriptSegment> DeserializeSegments(string json)
    {
        var stored = JsonSerializer.Deserialize<IReadOnlyCollection<StoredTranscriptSegment>>(json, JsonOptions) ?? [];
        return stored.Select(segment => new TranscriptSegment(segment.Id, segment.SpeakerName, TimeSpan.FromTicks(segment.StartTicks), TimeSpan.FromTicks(segment.EndTicks), segment.Text, segment.CleanText)).ToList();
    }

    private sealed record StoredTranscriptSegment(Guid Id, string SpeakerName, long StartTicks, long EndTicks, string Text, string CleanText);
}
