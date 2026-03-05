namespace Tripletex.Api.Handlers;

internal sealed class PathRewriteHandler(IReadOnlyDictionary<string, string> pathMappings) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            var path = request.RequestUri.AbsolutePath;
            var rewritten = RewritePath(path);

            if (rewritten != path)
            {
                var builder = new UriBuilder(request.RequestUri) { Path = rewritten };
                request.RequestUri = builder.Uri;
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    internal string RewritePath(string path)
    {
        foreach (var (sanitized, original) in pathMappings)
        {
            if (path.Contains(sanitized, StringComparison.OrdinalIgnoreCase))
                return path.Replace(sanitized, original, StringComparison.OrdinalIgnoreCase);
        }

        return path;
    }
}
