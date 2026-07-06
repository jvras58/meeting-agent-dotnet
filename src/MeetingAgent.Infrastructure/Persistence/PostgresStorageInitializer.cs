using MeetingAgent.Application.Ports;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresStorageInitializer : IStorageInitializer
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresStorageInitializer> _logger;

    public PostgresStorageInitializer(
        PostgresConnectionFactory connectionFactory,
        ILogger<PostgresStorageInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
        CREATE TABLE IF NOT EXISTS meetings (
            id UUID PRIMARY KEY,
            title TEXT NOT NULL,
            external_meeting_id TEXT NULL,
            online_meeting_id TEXT NULL,
            join_web_url TEXT NULL,
            organizer_email TEXT NULL,
            start_time TIMESTAMPTZ NULL,
            end_time TIMESTAMPTZ NULL,
            status INTEGER NOT NULL,
            failure_reason TEXT NULL,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );

        CREATE TABLE IF NOT EXISTS transcripts (
            id UUID PRIMARY KEY,
            meeting_id UUID NOT NULL UNIQUE REFERENCES meetings(id) ON DELETE CASCADE,
            source TEXT NOT NULL,
            language TEXT NOT NULL,
            raw_content TEXT NOT NULL,
            status INTEGER NOT NULL,
            segments_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );

        CREATE TABLE IF NOT EXISTS summaries (
            id UUID PRIMARY KEY,
            meeting_id UUID NOT NULL UNIQUE REFERENCES meetings(id) ON DELETE CASCADE,
            executive_summary TEXT NOT NULL,
            markdown TEXT NOT NULL,
            main_points_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            decisions_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            action_items_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            risks_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            open_questions_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_meetings_created_at ON meetings(created_at DESC);
        CREATE INDEX IF NOT EXISTS ix_transcripts_meeting_id ON transcripts(meeting_id);
        CREATE INDEX IF NOT EXISTS ix_summaries_meeting_id ON summaries(meeting_id);
        """;

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("PostgreSQL schema initialized.");
    }
}
