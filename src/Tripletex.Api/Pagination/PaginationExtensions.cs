using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tripletex.Api.Pagination;

public static class PaginationExtensions
{
    public static async IAsyncEnumerable<T> PaginateAsync<T>(
        Func<int, int, CancellationToken, Task<HttpResponseMessage>> requestFactory,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var from = 0;

        while (true)
        {
            using var response = await requestFactory(from, pageSize, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = JsonSerializer.Deserialize<ListResponse<T>>(json);

            if (page?.Values is null || page.Values.Count == 0)
                yield break;

            foreach (var item in page.Values)
                yield return item;

            if (page.FullResultSize <= from + page.Values.Count)
                yield break;

            from += page.Values.Count;
        }
    }
}

public sealed class ListResponse<T>
{
    [JsonPropertyName("fullResultSize")]
    public int FullResultSize { get; set; }

    [JsonPropertyName("from")]
    public int From { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("values")]
    public List<T>? Values { get; set; }
}

public sealed class SingleResponse<T>
{
    [JsonPropertyName("value")]
    public T? Value { get; set; }
}
