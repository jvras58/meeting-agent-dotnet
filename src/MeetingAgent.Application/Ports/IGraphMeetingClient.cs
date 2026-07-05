namespace MeetingAgent.Application.Ports;

public interface IGraphMeetingClient
{
    Task<string?> DownloadTranscriptAsync(string organizerUserId, string onlineMeetingId, string transcriptId, CancellationToken cancellationToken = default);
}
