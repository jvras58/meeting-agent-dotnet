namespace MeetingAgent.Contracts.Responses;

public sealed record SummaryResponse(
    Guid Id,
    Guid MeetingId,
    string ExecutiveSummary,
    IReadOnlyCollection<string> MainPoints,
    IReadOnlyCollection<DecisionResponse> Decisions,
    IReadOnlyCollection<ActionItemResponse> ActionItems,
    IReadOnlyCollection<RiskResponse> Risks,
    IReadOnlyCollection<string> OpenQuestions,
    string Markdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record DecisionResponse(
    string Text,
    string? Context
);

public sealed record ActionItemResponse(
    string Task,
    string? Owner,
    DateOnly? DueDate,
    string Status
);

public sealed record RiskResponse(
    string Text,
    string? Severity
);
