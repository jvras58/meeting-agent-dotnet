using System.Net.Http.Json;
using MeetingAgent.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MeetingAgent.Infrastructure.Graph;

public sealed class ClientCredentialsTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly GraphOptions _options;
    private string? _cachedToken;
    private DateTimeOffset _expiresAt;

    public ClientCredentialsTokenProvider(HttpClient httpClient, IOptions<GraphOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _cachedToken;
        }

        if (string.IsNullOrWhiteSpace(_options.TenantId) || string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("Graph credentials are missing. Configure AZURE_TENANT_ID, AZURE_CLIENT_ID and AZURE_CLIENT_SECRET.");
        }

        var tokenEndpoint = $"https://login.microsoftonline.com/{_options.TenantId}/oauth2/v2.0/token";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Token response is empty.");

        _cachedToken = payload.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn));
        return _cachedToken;
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);
}
