using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class ProjectOperations(HttpClient http)
{
    public async Task<Project> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"project/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<Project>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Project {id} not found");
    }

    public async Task<ListResponse<Project>> SearchAsync(
        string? name = null,
        string? number = null,
        bool? isOffer = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (name is not null) parts.Add($"name={Uri.EscapeDataString(name)}");
        if (number is not null) parts.Add($"number={Uri.EscapeDataString(number)}");
        if (isOffer.HasValue) parts.Add($"isOffer={isOffer.Value.ToString().ToLowerInvariant()}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "project?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Project>>(url, ct);
        return response ?? new ListResponse<Project>();
    }

    public async IAsyncEnumerable<Project> ListAsync(
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in PaginationExtensions.PaginateAsync<Project>(
            (f, c, token) => http.GetAsync($"project?from={f}&count={c}" +
                (fields is not null ? $"&fields={Uri.EscapeDataString(fields)}" : ""), token),
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<Project> CreateAsync(ProjectCreate project, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("project", project, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Project>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create project");
    }

    public async Task<Project> UpdateAsync(int id, Project project, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"project/{id}", project, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Project>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update project");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"project/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}

public sealed class Project
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("number")] public string? Number { get; set; }
    [JsonPropertyName("startDate")] public string? StartDate { get; set; }
    [JsonPropertyName("endDate")] public string? EndDate { get; set; }
    [JsonPropertyName("projectManager")] public IdRef? ProjectManager { get; set; }
    [JsonPropertyName("isClosed")] public bool IsClosed { get; set; }
    [JsonPropertyName("isOffer")] public bool IsOffer { get; set; }
    [JsonPropertyName("customer")] public IdRef? Customer { get; set; }
    [JsonPropertyName("projectActivities")] public List<ProjectActivity>? ProjectActivities { get; set; }
}

public sealed class ProjectActivity
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("activity")] public ActivityRef? Activity { get; set; }
    [JsonPropertyName("isClosed")] public bool IsClosed { get; set; }
}

public sealed class ActivityRef
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
}

public sealed class ProjectCreate
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("number")] public string? Number { get; set; }
    [JsonPropertyName("startDate")] public required string StartDate { get; set; }
    [JsonPropertyName("endDate")] public string? EndDate { get; set; }
    [JsonPropertyName("projectManager")] public required IdRef ProjectManager { get; set; }
    [JsonPropertyName("customer")] public IdRef? Customer { get; set; }
}
