using MeetingAgent.Application.Ports;
using MeetingAgent.Application.Workflows;
using MeetingAgent.Contracts.Requests;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;

namespace MeetingAgent.Application.UseCases;

public sealed class ImportMeetingUseCase
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly ISummaryRepository _summaryRepository;
    private readonly MeetingSummaryWorkflow _workflow;

    public ImportMeetingUseCase(
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

    public async Task<Guid> ExecuteAsync(ImportMeetingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawTranscript))
        {
            throw new ArgumentException("RawTranscript is required.", nameof(request));
        }

        var meeting = Meeting.Create(
            request.Title,
            request.ExternalMeetingId,
            request.OnlineMeetingId,
            request.JoinWebUrl,
            request.OrganizerEmail,
            request.StartTime,
            request.EndTime);

        var transcript = Transcript.Create(
            meeting.Id,
            request.Source ?? "manual",
            request.Language ?? "pt-BR",
            request.RawTranscript);

        meeting.MarkTranscriptImported();
        var summary = _workflow.Execute(meeting, transcript, request.SourceFormat);

        await _meetingRepository.AddAsync(meeting, cancellationToken);
        await _transcriptRepository.AddAsync(transcript, cancellationToken);
        await _summaryRepository.AddOrReplaceAsync(summary, cancellationToken);
        await _meetingRepository.SaveChangesAsync(cancellationToken);
        await _transcriptRepository.SaveChangesAsync(cancellationToken);
        await _summaryRepository.SaveChangesAsync(cancellationToken);

        return meeting.Id;
    }
}
