using MeetingAgent.Application.Ports;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Contracts.Requests;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;
using Microsoft.Extensions.Logging;

namespace MeetingAgent.Application.UseCases;

public sealed class ImportMeetingUseCase
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITranscriptRepository _transcriptRepository;
    private readonly IMeetingProcessingJobPublisher _jobPublisher;
    private readonly ILogger<ImportMeetingUseCase> _logger;

    public ImportMeetingUseCase(
        IMeetingRepository meetingRepository,
        ITranscriptRepository transcriptRepository,
        IMeetingProcessingJobPublisher jobPublisher,
        ILogger<ImportMeetingUseCase> logger)
    {
        _meetingRepository = meetingRepository;
        _transcriptRepository = transcriptRepository;
        _jobPublisher = jobPublisher;
        _logger = logger;
    }

    public async Task<Guid> ExecuteAsync(ImportMeetingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawTranscript))
        {
            throw new ArgumentException("RawTranscript is required.", nameof(request));
        }

        _logger.LogInformation(
            "Importing meeting. Title={Title}, ExternalMeetingId={ExternalMeetingId}, Source={Source}, SourceFormat={SourceFormat}, RawTranscriptLength={RawTranscriptLength}.",
            request.Title,
            request.ExternalMeetingId,
            request.Source ?? "manual",
            request.SourceFormat ?? "auto",
            request.RawTranscript.Length);

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
        meeting.MarkQueued();

        await _meetingRepository.AddAsync(meeting, cancellationToken);
        await _transcriptRepository.AddAsync(transcript, cancellationToken);
        await _meetingRepository.SaveChangesAsync(cancellationToken);
        await _transcriptRepository.SaveChangesAsync(cancellationToken);

        await _jobPublisher.PublishAsync(
            new MeetingProcessingRequested(
                meeting.Id,
                request.SourceFormat ?? "text",
                DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation(
            "Meeting imported and queued for worker processing. MeetingId={MeetingId}.",
            meeting.Id);

        return meeting.Id;
    }
}
