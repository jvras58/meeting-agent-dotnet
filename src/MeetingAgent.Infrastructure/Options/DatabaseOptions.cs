namespace MeetingAgent.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; set; } = "postgres";
    public string ConnectionString { get; set; } = "Host=postgres;Port=5432;Database=meeting_agent;Username=postgres;Password=postgres";
}
