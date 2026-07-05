using MeetingAgent.Application.Ports;
using MeetingAgent.Application.Workflows;

namespace MeetingAgent.Application.UseCases;

public sealed class ProcessMeetingUseCase
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly ISummaryRepository _summaryRepository;
    private readonly MeetingSummaryWorkflow _workflow;

    public ProcessMeetingUseCase(
        IMeetingRepository meetingRepository,
        ITranscriptRepository transcriptRepository,
        ISummaryRepository summaryRepository,
        MeetingSummaryWorkflow workflow)
    {
        _meetingRepository = meetingRepository;
        _transcriptRepository = transcriptRepository;
        _summaryRepository = summaryRepository;
        _workflow = workflow;
    }

    public async Task ExecuteAsync(Guid meetingId, string? sourceFormat, CancellationToken cancellationToken = default)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId, cancellationToken)
            ?? throw new InvalidOperationException($"Meeting {meetingId} not found.");

        var transcript = await _transcriptRepository.GetByMeetingIdAsync(meetingId, cancellationToken)
            ?? throw new InvalidOperationException($"Transcript for meeting {meetingId} not found.");

        var summary = _workflow.Execute(meeting, transcript, sourceFormat);
        await _summaryRepository.AddOrReplaceAsync(summary, cancellationToken);
        await _summaryRepository.SaveChangesAsync(cancellationToken);
    }
}
