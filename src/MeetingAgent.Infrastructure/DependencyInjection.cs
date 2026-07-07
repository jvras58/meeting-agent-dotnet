using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Ai;
using MeetingAgent.Infrastructure.Graph;
using MeetingAgent.Infrastructure.Jobs;
using MeetingAgent.Infrastructure.Options;
using MeetingAgent.Infrastructure.Persistence;
using MeetingAgent.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GraphOptions>(options =>
        {
            options.TenantId = configuration["AZURE_TENANT_ID"] ?? configuration[$"{GraphOptions.SectionName}:TenantId"] ?? string.Empty;
            options.ClientId = configuration["AZURE_CLIENT_ID"] ?? configuration[$"{GraphOptions.SectionName}:ClientId"] ?? string.Empty;
            options.ClientSecret = configuration["AZURE_CLIENT_SECRET"] ?? configuration[$"{GraphOptions.SectionName}:ClientSecret"] ?? string.Empty;
            options.BaseUrl = configuration["GRAPH_BASE_URL"] ?? configuration[$"{GraphOptions.SectionName}:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";
        });

        var aiTimeoutSeconds = GetInt(configuration, "AI_TIMEOUT_SECONDS", $"{AiOptions.SectionName}:TimeoutSeconds", 300);

        services.Configure<AiOptions>(options =>
        {
            options.Provider = configuration["AI_PROVIDER"] ?? configuration[$"{AiOptions.SectionName}:Provider"] ?? "heuristic";
            options.Model = configuration["AI_MODEL"] ?? configuration[$"{AiOptions.SectionName}:Model"] ?? "qwen3:8b";
            options.BaseUrl = configuration["AI_BASE_URL"] ?? configuration[$"{AiOptions.SectionName}:BaseUrl"] ?? "http://ollama:11434";
            options.TimeoutSeconds = aiTimeoutSeconds;
        });

        services.Configure<DatabaseOptions>(options =>
        {
            options.Provider = configuration["DATABASE_PROVIDER"] ?? configuration[$"{DatabaseOptions.SectionName}:Provider"] ?? "postgres";
            options.ConnectionString = configuration["DATABASE_URL"] ?? configuration[$"{DatabaseOptions.SectionName}:ConnectionString"] ?? "Host=postgres;Port=5432;Database=meeting_agent;Username=postgres;Password=postgres";
        });

        services.Configure<RabbitMqOptions>(options =>
        {
            options.Host = configuration["RABBITMQ_HOST"] ?? configuration[$"{RabbitMqOptions.SectionName}:Host"] ?? "rabbitmq";
            options.Port = GetInt(configuration, "RABBITMQ_PORT", $"{RabbitMqOptions.SectionName}:Port", 5672);
            options.User = configuration["RABBITMQ_USER"] ?? configuration[$"{RabbitMqOptions.SectionName}:User"] ?? "guest";
            options.Password = configuration["RABBITMQ_PASSWORD"] ?? configuration[$"{RabbitMqOptions.SectionName}:Password"] ?? "guest";
            options.QueueName = configuration["RABBITMQ_QUEUE"] ?? configuration[$"{RabbitMqOptions.SectionName}:QueueName"] ?? "meeting.processing.requested";
        });

        services.AddSingleton<IClock, SystemClock>();

        var databaseProvider = configuration["DATABASE_PROVIDER"] ?? "postgres";
        if (string.Equals(databaseProvider, "memory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IMeetingRepository, InMemoryMeetingRepository>();
            services.AddSingleton<ITranscriptRepository, InMemoryTranscriptRepository>();
            services.AddSingleton<ISummaryRepository, InMemorySummaryRepository>();
        }
        else
        {
            services.AddSingleton<PostgresConnectionFactory>();
            services.AddSingleton<IStorageInitializer, PostgresStorageInitializer>();
            services.AddScoped<IMeetingRepository, PostgresMeetingRepository>();
            services.AddScoped<ITranscriptRepository, PostgresTranscriptRepository>();
            services.AddScoped<ISummaryRepository, PostgresSummaryRepository>();
        }

        services.AddSingleton<IMeetingProcessingJobPublisher, RabbitMqMeetingProcessingJobPublisher>();

        services.AddHttpClient<ClientCredentialsTokenProvider>();
        services.AddHttpClient<IGraphMeetingClient, GraphMeetingClient>();

        var provider = configuration["AI_PROVIDER"] ?? "heuristic";
        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiChatService, OllamaChatService>((sp, client) =>
            {
                var baseUrl = configuration["AI_BASE_URL"] ?? "http://ollama:11434";

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(30, aiTimeoutSeconds));
            });
        }
        else
        {
            services.AddSingleton<IAiChatService, HeuristicAiChatService>();
        }

        return services;
    }

    private static int GetInt(IConfiguration configuration, string environmentKey, string configurationKey, int defaultValue)
    {
        return int.TryParse(configuration[environmentKey] ?? configuration[configurationKey], out var value)
            ? value
            : defaultValue;
    }
}
