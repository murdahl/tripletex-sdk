using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tripletex.Api.Handlers;

namespace Tripletex.Api.Tests.Unit.Handlers;

public class RateLimitHandlerTests
{
    [Fact]
    public async Task NonRateLimited_PassesThrough()
    {
        var handler = CreateHandler([HttpStatusCode.OK]);
        var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimited_RetriesAndSucceeds()
    {
        var handler = CreateHandler([HttpStatusCode.TooManyRequests, HttpStatusCode.OK]);
        var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RateLimited_ExhaustsRetries_ReturnsLastResponse()
    {
        var handler = CreateHandler([
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.TooManyRequests
        ]);
        var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/test");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static RateLimitHandler CreateHandler(HttpStatusCode[] responses)
    {
        var inner = new SequentialMockHandler(responses);
        return new RateLimitHandler(3, TimeSpan.FromMilliseconds(1), NullLogger.Instance)
        {
            InnerHandler = inner
        };
    }
}

internal class SequentialMockHandler(HttpStatusCode[] statusCodes) : HttpMessageHandler
{
    private int _callIndex;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var index = Math.Min(_callIndex++, statusCodes.Length - 1);
        return Task.FromResult(new HttpResponseMessage(statusCodes[index]));
    }
}
