using System.Net;
using Microsoft.Extensions.Logging;

namespace Tripletex.Api.Handlers;

internal sealed class RateLimitHandler(int maxRetries, TimeSpan baseDelay, ILogger logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await base.SendAsync(CloneRequest(request), cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                return response;

            if (attempt == maxRetries)
                return response;

            var delay = GetRetryDelay(response, attempt);
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

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        clone.Content = original.Content;
        clone.Version = original.Version;

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var prop in original.Options)
            ((IDictionary<string, object?>)clone.Options).Add(prop.Key, prop.Value);

        return clone;
    }
}
