using System.Net;
using Microsoft.Extensions.Logging;

namespace Tripletex.Api.Handlers;

internal sealed class RateLimitHandler(int maxRetries, TimeSpan baseDelay, ILogger logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        byte[]? contentBytes = null;
        string? contentMediaType = null;
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? contentHeaders = null;

        if (request.Content is not null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentMediaType = request.Content.Headers.ContentType?.ToString();
            contentHeaders = request.Content.Headers.ToList();
        }

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await base.SendAsync(
                CloneRequest(request, contentBytes, contentMediaType, contentHeaders), cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                return response;

            if (attempt == maxRetries)
                return response;

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            logger.LogWarning("Rate limited (429). Retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                delay.TotalMilliseconds, attempt + 1, maxRetries);

            await Task.Delay(delay, cancellationToken);
        }

        throw new InvalidOperationException("Unreachable");
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.TryGetValues("X-Rate-Limit-Reset", out var resetValues))
        {
            var resetStr = resetValues.FirstOrDefault();
            if (int.TryParse(resetStr, out var resetSeconds) && resetSeconds > 0)
                return TimeSpan.FromSeconds(Math.Min(resetSeconds, 60));
        }

        return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
    }

    private static HttpRequestMessage CloneRequest(
        HttpRequestMessage original,
        byte[]? contentBytes,
        string? contentMediaType,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? contentHeaders)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        clone.Version = original.Version;

        if (contentBytes is not null)
        {
            clone.Content = new ByteArrayContent(contentBytes);
            if (contentHeaders is not null)
            {
                foreach (var header in contentHeaders)
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in original.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        return clone;
    }
}
