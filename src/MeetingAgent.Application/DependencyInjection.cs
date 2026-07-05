using MeetingAgent.Application.Services;
using MeetingAgent.Application.UseCases;
using MeetingAgent.Application.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingAgent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<TextNoiseCleaner>();
        services.AddSingleton<TranscriptNormalizer>();
        services.AddSingleton<HeuristicSummaryBuilder>();
        services.AddSingleton<MarkdownSummaryRenderer>();
        services.AddSingleton<MeetingSummaryWorkflow>();
        services.AddScoped<ImportMeetingUseCase>();
        services.AddScoped<ProcessMeetingUseCase>();
        services.AddScoped<GetMeetingSummaryUseCase>();
        return services;
    }
}
