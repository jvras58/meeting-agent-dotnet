using MeetingAgent.Infrastructure.Configuration;
using MeetingAgent.Application;
using MeetingAgent.Infrastructure;
using MeetingAgent.Worker;

DotEnv.Load();

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MeetingProcessingWorker>();

var host = builder.Build();
host.Run();
