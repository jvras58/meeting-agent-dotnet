using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MeetingAgent.Infrastructure.Persistence;

public sealed class PostgresConnectionFactory
{
    private readonly DatabaseOptions _options;

    public PostgresConnectionFactory(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
