using MeetingAgent.Application.Ports;
using MeetingAgent.Application.Workflows;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Application.UseCases;

public sealed class ProcessMeetingUseCase
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly ISummaryRepository _summaryRepository;
    private readonly MeetingSummaryWorkflow _workflow;
    private readonly ILogger<ProcessMeetingUseCase> _logger;

    public ProcessMeetingUseCase(
        IMeetingRepository meetingRepository,
        ITranscriptRepository transcriptRepository,
        ISummaryRepository summaryRepository,
        MeetingSummaryWorkflow workflow,
        ILogger<ProcessMeetingUseCase> logger)
    {
        _meetingRepository = meetingRepository;
        _transcriptRepository = transcriptRepository;
        _summaryRepository = summaryRepository;
        _workflow = workflow;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid meetingId, string? sourceFormat, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing existing meeting. MeetingId={MeetingId}, SourceFormat={SourceFormat}.",
            meetingId,
            sourceFormat ?? "auto");

        var meeting = await _meetingRepository.GetByIdAsync(meetingId, cancellationToken)
            ?? throw new InvalidOperationException($"Meeting {meetingId} not found.");

        var transcript = await _transcriptRepository.GetByMeetingIdAsync(meetingId, cancellationToken)
            ?? throw new InvalidOperationException($"Transcript for meeting {meetingId} not found.");

        var summary = await _workflow.ExecuteAsync(meeting, transcript, sourceFormat, cancellationToken);
        await _summaryRepository.AddOrReplaceAsync(summary, cancellationToken);
        await _summaryRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Existing meeting processed. MeetingId={MeetingId}, SummaryId={SummaryId}.",
            meetingId,
            summary.Id);
    }
}
