namespace MeetingAgent.Domain.Summaries;

public sealed record ActionItem(
    string Task,
    string? Owner,
    DateOnly? DueDate,
    ActionItemStatus Status = ActionItemStatus.Pending);
