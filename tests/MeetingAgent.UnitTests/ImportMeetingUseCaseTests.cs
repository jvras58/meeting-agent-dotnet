using MeetingAgent.Application.Ports;
using MeetingAgent.Application.UseCases;
using MeetingAgent.Contracts.Events;
using MeetingAgent.Contracts.Requests;
using MeetingAgent.Domain.Meetings;
using MeetingAgent.Domain.Transcripts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MeetingAgent.UnitTests;

public sealed class ImportMeetingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_saves_meeting_transcript_and_outbox_message()
    {
        var store = new CapturingMeetingProcessingRequestStore();
        var useCase = new ImportMeetingUseCase(store, NullLogger<ImportMeetingUseCase>.Instance);

        var meetingId = await useCase.ExecuteAsync(new ImportMeetingRequest(
            Title: "Planning",
            RawTranscript: "Jonathas: vamos fechar a fase 1.",
            SourceFormat: "text"));

        Assert.Equal(meetingId, store.Meeting?.Id);
        Assert.Equal(MeetingStatus.Queued, store.Meeting?.Status);
        Assert.Equal(meetingId, store.Transcript?.MeetingId);
        Assert.Equal(meetingId, store.Message?.MeetingId);
        Assert.Equal("text", store.Message?.SourceFormat);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_empty_transcript()
    {
        var store = new CapturingMeetingProcessingRequestStore();
        var useCase = new ImportMeetingUseCase(store, NullLogger<ImportMeetingUseCase>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new ImportMeetingRequest(
            Title: "Planning",
            RawTranscript: "   ")));
    }

    private sealed class CapturingMeetingProcessingRequestStore : IMeetingProcessingRequestStore
    {
        public Meeting? Meeting { get; private set; }
        public Transcript? Transcript { get; private set; }
        public MeetingProcessingRequested? Message { get; private set; }

        public Task SaveImportedMeetingAndQueueAsync(
            Meeting meeting,
            Transcript transcript,
            MeetingProcessingRequested message,
            CancellationToken cancellationToken = default)
        {
            Meeting = meeting;
            Transcript = transcript;
            Message = message;
            return Task.CompletedTask;
        }

        public Task<bool> QueueExistingMeetingAsync(
            Guid meetingId,
            MeetingProcessingRequested message,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
