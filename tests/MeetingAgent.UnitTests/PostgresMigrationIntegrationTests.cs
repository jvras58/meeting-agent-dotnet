using MeetingAgent.Infrastructure.Options;
using MeetingAgent.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MeetingAgent.UnitTests;

public sealed class PostgresMigrationIntegrationTests
{
    [Fact]
    public async Task InitializeAsync_creates_schema_migrations_and_outbox_when_database_is_available()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var factory = new PostgresConnectionFactory(Options.Create(new DatabaseOptions
        {
            Provider = "postgres",
            ConnectionString = connectionString
        }));

        var initializer = new PostgresStorageInitializer(
            factory,
            NullLogger<PostgresStorageInitializer>.Instance);

        await initializer.InitializeAsync();

        await using var connection = await factory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
        SELECT
            to_regclass('public.schema_migrations') IS NOT NULL,
            to_regclass('public.outbox_messages') IS NOT NULL,
            (SELECT COUNT(*) FROM schema_migrations) >= 2
        """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
        Assert.True(reader.GetBoolean(2));
    }
}
