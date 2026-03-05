namespace Tripletex.Api.Handlers;

internal sealed class PathRewriteHandler : DelegatingHandler
{
    private readonly (string Sanitized, string Original)[] _mappings;

    public PathRewriteHandler(IReadOnlyDictionary<string, string> pathMappings)
    {
        _mappings = pathMappings
            .OrderByDescending(kvp => kvp.Key.Length)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToArray();
    }

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
        foreach (var (sanitized, original) in _mappings)
        {
            if (path.EndsWith(sanitized, StringComparison.OrdinalIgnoreCase))
                return string.Concat(path.AsSpan(0, path.Length - sanitized.Length), original);
        }

        return path;
    }
}
