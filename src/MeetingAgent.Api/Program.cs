using MeetingAgent.Infrastructure.Configuration;
using MeetingAgent.Application;
using MeetingAgent.Application.Ports;
using MeetingAgent.Application.UseCases;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Contracts.Requests;
using MeetingAgent.Contracts.Responses;
using MeetingAgent.Infrastructure;

DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

var storageInitializer = app.Services.GetService<IStorageInitializer>();
if (storageInitializer is not null)
{
    await storageInitializer.InitializeAsync();
}

startupLogger.LogInformation(
    "MeetingAgent.Api started. Environment={Environment}, AiProvider={AiProvider}, AiModel={AiModel}.",
    app.Environment.EnvironmentName,
    app.Configuration["AI_PROVIDER"] ?? "heuristic",
    app.Configuration["AI_MODEL"] ?? "qwen3:8b");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "meeting-agent-api" }))
    .WithName("Health");

app.MapGet("/ready", () => Results.Ok(new { status = "ready" }))
    .WithName("Ready");

app.MapPost("/webhooks/graph", async (HttpRequest request, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("GraphWebhook");

    if (request.Query.TryGetValue("validationToken", out var validationToken))
    {
        logger.LogInformation("Graph webhook validation requested.");
        return Results.Text(validationToken.ToString(), "text/plain");
    }

    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    logger.LogInformation("Graph webhook received: {PayloadLength} bytes", body.Length);

    // TODO: Validar clientState, persistir evento e publicar job idempotente.
    return Results.Accepted();
})
.WithName("GraphWebhook");

app.MapPost("/meetings/import", async (ImportMeetingRequest input, ImportMeetingUseCase useCase, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("ImportMeetingEndpoint");
    logger.LogInformation("Import meeting request received. Title={Title}, Source={Source}.", input.Title, input.Source ?? "manual");

    var id = await useCase.ExecuteAsync(input, cancellationToken);
    return Results.Accepted($"/meetings/{id}/summary", new { id, status = "Queued" });
})
.WithName("ImportMeeting");

app.MapGet("/meetings", async (IMeetingRepository repository, CancellationToken cancellationToken) =>
{
    var meetings = await repository.ListAsync(cancellationToken);
    var response = meetings.Select(m => new MeetingResponse(
        m.Id,
        m.Title,
        m.Status.ToString(),
        m.OrganizerEmail,
        m.StartTime,
        m.EndTime,
        m.CreatedAt,
        m.UpdatedAt));

    return Results.Ok(response);
})
.WithName("ListMeetings");

app.MapGet("/meetings/{id:guid}", async (Guid id, IMeetingRepository repository, CancellationToken cancellationToken) =>
{
    var meeting = await repository.GetByIdAsync(id, cancellationToken);
    if (meeting is null) return Results.NotFound();

    return Results.Ok(new MeetingResponse(
        meeting.Id,
        meeting.Title,
        meeting.Status.ToString(),
        meeting.OrganizerEmail,
        meeting.StartTime,
        meeting.EndTime,
        meeting.CreatedAt,
        meeting.UpdatedAt));
})
.WithName("GetMeeting");

app.MapPost("/meetings/{id:guid}/process", async (
    Guid id,
    IMeetingProcessingRequestStore requestStore,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("ProcessMeetingEndpoint");
    logger.LogInformation("Queue process meeting request received. MeetingId={MeetingId}.", id);

    var queued = await requestStore.QueueExistingMeetingAsync(
        id,
        new MeetingProcessingRequested(id, "text", DateTimeOffset.UtcNow),
        cancellationToken);

    if (!queued) return Results.NotFound();

    return Results.Accepted($"/meetings/{id}/summary", new { id, status = "Queued" });
})
.WithName("ProcessMeeting");

app.MapGet("/meetings/{id:guid}/summary", async (Guid id, GetMeetingSummaryUseCase useCase, CancellationToken cancellationToken) =>
{
    var summary = await useCase.ExecuteAsync(id, cancellationToken);
    if (summary is null) return Results.NotFound();

    return Results.Ok(new SummaryResponse(
        summary.Id,
        summary.MeetingId,
        summary.ExecutiveSummary,
        summary.MainPoints.ToList(),
        summary.Decisions
            .Select(decision => new DecisionResponse(
                decision.Text,
                decision.Context
            ))
            .ToList(),
        summary.ActionItems
            .Select(actionItem => new ActionItemResponse(
                actionItem.Task,
                actionItem.Owner,
                actionItem.DueDate,
                actionItem.Status.ToString()
            ))
            .ToList(),
        summary.Risks
            .Select(risk => new RiskResponse(
                risk.Text,
                risk.Severity
            ))
            .ToList(),
        summary.OpenQuestions.ToList(),
        summary.Markdown,
        summary.CreatedAt,
        summary.UpdatedAt
    ));
})
.WithName("GetSummary");

app.Run();
