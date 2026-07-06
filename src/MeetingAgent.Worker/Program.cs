using MeetingAgent.Infrastructure.Configuration;
using MeetingAgent.Application;
using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure;
using MeetingAgent.Worker;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MeetingProcessingWorker>();

var host = builder.Build();
var startupLogger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

var storageInitializer = host.Services.GetService<IStorageInitializer>();
if (storageInitializer is not null)
{
    await storageInitializer.InitializeAsync();
}

startupLogger.LogInformation(
    "MeetingAgent.Worker started. Environment={Environment}, AiProvider={AiProvider}, AiModel={AiModel}.",
    builder.Environment.EnvironmentName,
    builder.Configuration["AI_PROVIDER"] ?? "heuristic",
    builder.Configuration["AI_MODEL"] ?? "qwen3:8b");

host.Run();
