using System.Net.Http.Headers;
using MeetingAgent.Application.Ports;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingAgent.Infrastructure.Graph;

public sealed class GraphMeetingClient : IGraphMeetingClient
{
    private readonly HttpClient _httpClient;
    private readonly ClientCredentialsTokenProvider _tokenProvider;
    private readonly GraphOptions _options;

    public GraphMeetingClient(HttpClient httpClient, ClientCredentialsTokenProvider tokenProvider, IOptions<GraphOptions> options)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    public async Task<string?> DownloadTranscriptAsync(string organizerUserId, string onlineMeetingId, string transcriptId, CancellationToken cancellationToken = default)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_options.BaseUrl.TrimEnd('/')}/users/{Uri.EscapeDataString(organizerUserId)}/onlineMeetings/{Uri.EscapeDataString(onlineMeetingId)}/transcripts/{Uri.EscapeDataString(transcriptId)}/content?$format=text/vtt");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
