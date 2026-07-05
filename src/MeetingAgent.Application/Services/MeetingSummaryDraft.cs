using MeetingAgent.Domain.Summaries;

namespace MeetingAgent.Application.Services;

public sealed record MeetingSummaryDraft(
    string ExecutiveSummary,
    IReadOnlyCollection<string> MainPoints,
    IReadOnlyCollection<Decision> Decisions,
    IReadOnlyCollection<ActionItem> ActionItems,
    IReadOnlyCollection<Risk> Risks,
    IReadOnlyCollection<string> OpenQuestions);
