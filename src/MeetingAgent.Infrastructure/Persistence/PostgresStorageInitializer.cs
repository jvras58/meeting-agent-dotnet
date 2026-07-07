using MeetingAgent.Application.Ports;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresStorageInitializer : IStorageInitializer
{
    private static readonly IReadOnlyCollection<MigrationDefinition> Migrations =
    [
        new(
            "202607070001_initial_meeting_schema",
            """
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
            """),
        new(
            "202607070002_meeting_processing_outbox",
            """
            CREATE TABLE IF NOT EXISTS outbox_messages (
                id UUID PRIMARY KEY,
                type TEXT NOT NULL,
                payload JSONB NOT NULL,
                status TEXT NOT NULL DEFAULT 'pending',
                attempts INTEGER NOT NULL DEFAULT 0,
                available_at TIMESTAMPTZ NOT NULL,
                published_at TIMESTAMPTZ NULL,
                error TEXT NULL,
                created_at TIMESTAMPTZ NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_outbox_messages_pending
                ON outbox_messages(status, available_at, created_at)
                WHERE status IN ('pending', 'failed');

            CREATE INDEX IF NOT EXISTS ix_outbox_messages_type
                ON outbox_messages(type);
            """)
    ];

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
        await using var bootstrap = connection.CreateCommand();
        bootstrap.CommandText = """
        CREATE TABLE IF NOT EXISTS schema_migrations (
            version TEXT PRIMARY KEY,
            applied_at TIMESTAMPTZ NOT NULL
        );
        """;
        await bootstrap.ExecuteNonQueryAsync(cancellationToken);

        foreach (var migration in Migrations.OrderBy(migration => migration.Version, StringComparer.Ordinal))
        {
            if (await IsAppliedAsync(connection, migration.Version, cancellationToken))
            {
                continue;
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var migrationCommand = connection.CreateCommand();
            migrationCommand.Transaction = transaction;
            migrationCommand.CommandText = migration.Sql;
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var markApplied = connection.CreateCommand();
            markApplied.Transaction = transaction;
            markApplied.CommandText = "INSERT INTO schema_migrations (version, applied_at) VALUES (@version, @applied_at)";
            markApplied.Parameters.AddWithValue("version", migration.Version);
            markApplied.Parameters.AddWithValue("applied_at", DateTimeOffset.UtcNow);
            await markApplied.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Applied PostgreSQL migration {MigrationVersion}.", migration.Version);
        }

        _logger.LogInformation("PostgreSQL migrations initialized.");
    }

    private static async Task<bool> IsAppliedAsync(
        Npgsql.NpgsqlConnection connection,
        string version,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM schema_migrations WHERE version = @version";
        command.Parameters.AddWithValue("version", version);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    private sealed record MigrationDefinition(string Version, string Sql);
}
