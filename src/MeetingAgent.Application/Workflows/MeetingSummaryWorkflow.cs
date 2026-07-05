using MeetingAgent.Application.Services;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Summaries;
using MeetingAgent.Domain.Transcripts;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Application.Workflows;

public sealed class MeetingSummaryWorkflow
{
    private readonly TranscriptNormalizer _normalizer;
    private readonly AiMeetingSummaryBuilder _summaryBuilder;
    private readonly MarkdownSummaryRenderer _renderer;
    private readonly ILogger<MeetingSummaryWorkflow> _logger;

    public MeetingSummaryWorkflow(
        TranscriptNormalizer normalizer,
        AiMeetingSummaryBuilder summaryBuilder,
        MarkdownSummaryRenderer renderer,
        ILogger<MeetingSummaryWorkflow> logger)
    {
        _normalizer = normalizer;
        _summaryBuilder = summaryBuilder;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<MeetingSummary> ExecuteAsync(
        Meeting meeting,
        Transcript transcript,
        string? sourceFormat,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting meeting summary workflow. MeetingId={MeetingId}, Source={Source}, SourceFormat={SourceFormat}, RawTranscriptLength={RawTranscriptLength}.",
            meeting.Id,
            transcript.Source,
            sourceFormat ?? "auto",
            transcript.RawContent.Length);

        meeting.MarkProcessing();

        var segments = _normalizer.Normalize(transcript.RawContent, sourceFormat);
        transcript.ReplaceSegments(segments);

        _logger.LogInformation(
            "Transcript normalized. MeetingId={MeetingId}, SegmentCount={SegmentCount}.",
            meeting.Id,
            segments.Count);

        var draft = await _summaryBuilder.BuildAsync(segments, cancellationToken);
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

        _logger.LogInformation(
            "Meeting summary workflow finished. MeetingId={MeetingId}, SummaryId={SummaryId}.",
            meeting.Id,
            summary.Id);

        return summary;
    }
}
