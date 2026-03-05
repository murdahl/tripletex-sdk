using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tripletex.Api.Authentication;

namespace Tripletex.Api.Tests.Unit.Authentication;

public class SessionTokenProviderTests
{
    [Fact]
    public async Task GetSessionAsync_CreatesToken_OnFirstCall()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            value = new
            {
                id = 1,
                encryptedId = 42,
                token = "test-session-token"
            }
        });

        var handler = new MockSessionHandler(responseBody);
        var httpClient = new HttpClient(handler);

        using var provider = new SessionTokenProvider(
            "consumer-token",
            "employee-token",
            "https://api-test.tripletex.tech/v2",
            TimeSpan.FromHours(1),
            httpClient,
            NullLogger.Instance);

        var (companyId, token) = await provider.GetSessionAsync();

        companyId.Should().Be(42);
        token.Should().Be("test-session-token");
    }

    [Fact]
    public async Task GetSessionAsync_CachesToken_OnSubsequentCalls()
    {
        var responseBody = JsonSerializer.Serialize(new
        {
            value = new { id = 1, encryptedId = 42, token = "test-token" }
        });

        var handler = new MockSessionHandler(responseBody);
        var httpClient = new HttpClient(handler);

        using var provider = new SessionTokenProvider(
            "consumer", "employee",
            "https://api-test.tripletex.tech/v2",
            TimeSpan.FromHours(1),
            httpClient,
            NullLogger.Instance);

        await provider.GetSessionAsync();
        await provider.GetSessionAsync();
        await provider.GetSessionAsync();

        handler.CallCount.Should().Be(1);
    }
}

internal class MockSessionHandler(string responseBody) : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
