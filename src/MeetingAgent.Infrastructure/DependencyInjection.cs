using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Ai;
using MeetingAgent.Infrastructure.Graph;
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

        services.Configure<AiOptions>(options =>
        {
            options.Provider = configuration["AI_PROVIDER"] ?? configuration[$"{AiOptions.SectionName}:Provider"] ?? "heuristic";
            options.Model = configuration["AI_MODEL"] ?? configuration[$"{AiOptions.SectionName}:Model"] ?? "qwen3:8b";
            options.BaseUrl = configuration["AI_BASE_URL"] ?? configuration[$"{AiOptions.SectionName}:BaseUrl"] ?? "http://localhost:11434";
        });

        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IMeetingRepository, InMemoryMeetingRepository>();
        services.AddSingleton<ITranscriptRepository, InMemoryTranscriptRepository>();
        services.AddSingleton<ISummaryRepository, InMemorySummaryRepository>();

        services.AddHttpClient<ClientCredentialsTokenProvider>();
        services.AddHttpClient<IGraphMeetingClient, GraphMeetingClient>();

        var provider = configuration["AI_PROVIDER"] ?? "heuristic";
        if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAiChatService, OllamaChatService>((sp, client) =>
            {
                var baseUrl = configuration["AI_BASE_URL"] ?? "http://localhost:11434";
                client.BaseAddress = new Uri(baseUrl);
            });
        }
        else
        {
            services.AddSingleton<IAiChatService, HeuristicAiChatService>();
        }

        return services;
    }
}
