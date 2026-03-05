using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class CustomerOperations(HttpClient http)
{
    public async Task<Customer> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"customer/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<Customer>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Customer {id} not found");
    }

    public async Task<ListResponse<Customer>> SearchAsync(
        string? name = null,
        string? customerAccountNumber = null,
        string? organizationNumber = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (name is not null) parts.Add($"name={Uri.EscapeDataString(name)}");
        if (customerAccountNumber is not null) parts.Add($"customerAccountNumber={Uri.EscapeDataString(customerAccountNumber)}");
        if (organizationNumber is not null) parts.Add($"organizationNumber={Uri.EscapeDataString(organizationNumber)}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "customer?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Customer>>(url, ct);
        return response ?? new ListResponse<Customer>();
    }

    public async IAsyncEnumerable<Customer> ListAsync(
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in PaginationExtensions.PaginateAsync<Customer>(
            (f, c, token) => http.GetAsync($"customer?from={f}&count={c}" +
                (fields is not null ? $"&fields={Uri.EscapeDataString(fields)}" : ""), token),
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<Customer> CreateAsync(CustomerCreate customer, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("customer", customer, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Customer>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create customer");
    }

    public async Task<Customer> UpdateAsync(int id, Customer customer, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"customer/{id}", customer, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Customer>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update customer");
    }
}

public sealed class Customer
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("organizationNumber")] public string? OrganizationNumber { get; set; }
    [JsonPropertyName("customerNumber")] public int CustomerNumber { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
    [JsonPropertyName("isCustomer")] public bool IsCustomer { get; set; }
    [JsonPropertyName("isSupplier")] public bool IsSupplier { get; set; }
}

public sealed class CustomerCreate
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("organizationNumber")] public string? OrganizationNumber { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phoneNumber")] public string? PhoneNumber { get; set; }
    [JsonPropertyName("isCustomer")] public bool IsCustomer { get; set; } = true;
}
