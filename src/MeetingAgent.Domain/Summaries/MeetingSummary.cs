using MeetingAgent.Domain.Common;

namespace MeetingAgent.Domain.Summaries;

public sealed class MeetingSummary : Entity
{
    private readonly List<string> _mainPoints = [];
    private readonly List<Decision> _decisions = [];
    private readonly List<ActionItem> _actionItems = [];
    private readonly List<Risk> _risks = [];
    private readonly List<string> _openQuestions = [];

    private MeetingSummary(Guid id, Guid meetingId, string executiveSummary, string markdown) : base(id)
    {
        MeetingId = meetingId;
        ExecutiveSummary = executiveSummary.Trim();
        Markdown = markdown.Trim();
    }

    public Guid MeetingId { get; }
    public string ExecutiveSummary { get; private set; }
    public string Markdown { get; private set; }
    public IReadOnlyCollection<string> MainPoints => _mainPoints.AsReadOnly();
    public IReadOnlyCollection<Decision> Decisions => _decisions.AsReadOnly();
    public IReadOnlyCollection<ActionItem> ActionItems => _actionItems.AsReadOnly();
    public IReadOnlyCollection<Risk> Risks => _risks.AsReadOnly();
    public IReadOnlyCollection<string> OpenQuestions => _openQuestions.AsReadOnly();

    public static MeetingSummary Create(
        Guid meetingId,
        string executiveSummary,
        string markdown,
        IEnumerable<string>? mainPoints = null,
        IEnumerable<Decision>? decisions = null,
        IEnumerable<ActionItem>? actionItems = null,
        IEnumerable<Risk>? risks = null,
        IEnumerable<string>? openQuestions = null)
    {
        if (meetingId == Guid.Empty) throw new ArgumentException("Meeting id is required.", nameof(meetingId));
        var summary = new MeetingSummary(Guid.NewGuid(), meetingId, executiveSummary, markdown);
        summary._mainPoints.AddRange(mainPoints ?? []);
        summary._decisions.AddRange(decisions ?? []);
        summary._actionItems.AddRange(actionItems ?? []);
        summary._risks.AddRange(risks ?? []);
        summary._openQuestions.AddRange(openQuestions ?? []);
        return summary;
    }

    public static MeetingSummary Restore(
        Guid id,
        Guid meetingId,
        string executiveSummary,
        string markdown,
        IEnumerable<string>? mainPoints,
        IEnumerable<Decision>? decisions,
        IEnumerable<ActionItem>? actionItems,
        IEnumerable<Risk>? risks,
        IEnumerable<string>? openQuestions,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var summary = new MeetingSummary(id, meetingId, executiveSummary, markdown)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        summary._mainPoints.AddRange(mainPoints ?? []);
        summary._decisions.AddRange(decisions ?? []);
        summary._actionItems.AddRange(actionItems ?? []);
        summary._risks.AddRange(risks ?? []);
        summary._openQuestions.AddRange(openQuestions ?? []);
        return summary;
    }
}
