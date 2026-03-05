using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Tripletex.Api.Authentication;

internal sealed class SessionTokenProvider : IDisposable
{
    private readonly string _consumerToken;
    private readonly string _employeeToken;
    private readonly string _baseUrl;
    private readonly TimeSpan _sessionLifetime;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private SessionInfo? _session;

    public SessionTokenProvider(
        string consumerToken,
        string employeeToken,
        string baseUrl,
        TimeSpan sessionLifetime,
        HttpClient httpClient,
        ILogger logger)
    {
        _consumerToken = consumerToken;
        _employeeToken = employeeToken;
        _baseUrl = baseUrl;
        _sessionLifetime = sessionLifetime;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(int CompanyId, string Token)> GetSessionAsync(CancellationToken ct = default)
    {
        var session = _session;
        if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            return (session.CompanyId, session.Token);

        await _semaphore.WaitAsync(ct);
        try
        {
            session = _session;
            if (session is not null && session.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                return (session.CompanyId, session.Token);

            _logger.LogDebug("Creating new Tripletex session token");

            var expirationDate = DateTimeOffset.UtcNow.Add(_sessionLifetime).ToString("yyyy-MM-dd");
            var url = $"{_baseUrl}/token/session/:create?consumerToken={Uri.EscapeDataString(_consumerToken)}&employeeToken={Uri.EscapeDataString(_employeeToken)}&expirationDate={expirationDate}";

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<SessionCreateResponse>(json)
                ?? throw new InvalidOperationException("Failed to deserialize session token response");

            var value = result.Value
                ?? throw new InvalidOperationException("Session token response missing value");

            _session = new SessionInfo(
                value.Token ?? throw new InvalidOperationException("Session token is null"),
                value.EncryptedId ?? throw new InvalidOperationException("Session token response missing encryptedId"),
                DateTimeOffset.UtcNow.Add(_sessionLifetime));

            _logger.LogDebug("Session token created, company ID: {CompanyId}, expires: {Expires}",
                _session.CompanyId, _session.ExpiresAt);

            return (_session.CompanyId, _session.Token);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed record SessionInfo(string Token, int CompanyId, DateTimeOffset ExpiresAt);

    private sealed class SessionCreateResponse
    {
        [JsonPropertyName("value")]
        public SessionTokenValue? Value { get; set; }
    }

    private sealed class SessionTokenValue
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("encryptedId")]
        public int? EncryptedId { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
