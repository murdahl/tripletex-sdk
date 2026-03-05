using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class ActivityOperations(HttpClient http)
{
    public async Task<Activity> GetAsync(int id, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<SingleResponse<Activity>>($"activity/{id}", ct);
        return response?.Value ?? throw new InvalidOperationException($"Activity {id} not found");
    }

    public async Task<ListResponse<Activity>> SearchAsync(
        string? name = null,
        int from = 0,
        int count = 1000,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (name is not null) parts.Add($"name={Uri.EscapeDataString(name)}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");

        var url = "activity?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Activity>>(url, ct);
        return response ?? new ListResponse<Activity>();
    }
}

public sealed class Activity
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("number")] public string? Number { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("isDisabled")] public bool IsDisabled { get; set; }
}
