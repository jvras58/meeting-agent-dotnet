using MeetingAgent.Application.Ports;
using MeetingAgent.Application.Services;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Summaries;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.Workflows;

public sealed class MeetingSummaryWorkflow
{
    private readonly TranscriptNormalizer _normalizer;
    private readonly HeuristicSummaryBuilder _summaryBuilder;
    private readonly MarkdownSummaryRenderer _renderer;

    public MeetingSummaryWorkflow(
        TranscriptNormalizer normalizer,
        HeuristicSummaryBuilder summaryBuilder,
        MarkdownSummaryRenderer renderer)
    {
        _normalizer = normalizer;
        _summaryBuilder = summaryBuilder;
        _renderer = renderer;
    }

    public MeetingSummary Execute(Meeting meeting, Transcript transcript, string? sourceFormat)
    {
        meeting.MarkProcessing();

        var segments = _normalizer.Normalize(transcript.RawContent, sourceFormat);
        transcript.ReplaceSegments(segments);

        var draft = _summaryBuilder.Build(segments);
        var markdown = _renderer.Render(meeting, draft);

        var summary = MeetingSummary.Create(
            meeting.Id,
            draft.ExecutiveSummary,
            markdown,
            draft.MainPoints,
            draft.Decisions,
            draft.ActionItems,
            draft.Risks,
            draft.OpenQuestions);

        meeting.MarkSummaryGenerated();
        return summary;
    }
}
