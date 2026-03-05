using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class SupplierOperations(HttpClient http)
{
    public async Task<Supplier> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"/supplier/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<Supplier>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Supplier {id} not found");
    }

    public async Task<ListResponse<Supplier>> SearchAsync(
        string? name = null,
        string? organizationNumber = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (name is not null) parts.Add($"name={Uri.EscapeDataString(name)}");
        if (organizationNumber is not null) parts.Add($"organizationNumber={Uri.EscapeDataString(organizationNumber)}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "/supplier?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Supplier>>(url, ct);
        return response ?? new ListResponse<Supplier>();
    }

    public async IAsyncEnumerable<Supplier> ListAsync(
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in PaginationExtensions.PaginateAsync<Supplier>(
            (f, c, token) => http.GetAsync($"/supplier?from={f}&count={c}" +
                (fields is not null ? $"&fields={Uri.EscapeDataString(fields)}" : ""), token),
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<Supplier> CreateAsync(SupplierCreate supplier, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/supplier", supplier, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Supplier>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create supplier");
    }

    public async Task<Supplier> UpdateAsync(int id, Supplier supplier, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"/supplier/{id}", supplier, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Supplier>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update supplier");
    }
}

public sealed class Supplier
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("organizationNumber")] public string? OrganizationNumber { get; set; }
    [JsonPropertyName("supplierNumber")] public int SupplierNumber { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
}

public sealed class SupplierCreate
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("organizationNumber")] public string? OrganizationNumber { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
}
