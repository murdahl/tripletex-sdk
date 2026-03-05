using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class EmployeeOperations(HttpClient http)
{
    public async Task<Employee> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"/employee/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<Employee>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Employee {id} not found");
    }

    public async Task<ListResponse<Employee>> SearchAsync(
        string? firstName = null,
        string? lastName = null,
        string? employeeNumber = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (firstName is not null) parts.Add($"firstName={Uri.EscapeDataString(firstName)}");
        if (lastName is not null) parts.Add($"lastName={Uri.EscapeDataString(lastName)}");
        if (employeeNumber is not null) parts.Add($"employeeNumber={Uri.EscapeDataString(employeeNumber)}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "/employee?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Employee>>(url, ct);
        return response ?? new ListResponse<Employee>();
    }

    public async IAsyncEnumerable<Employee> ListAsync(
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in PaginationExtensions.PaginateAsync<Employee>(
            (f, c, token) => http.GetAsync($"/employee?from={f}&count={c}" +
                (fields is not null ? $"&fields={Uri.EscapeDataString(fields)}" : ""), token),
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<Employee> CreateAsync(EmployeeCreate employee, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/employee", employee, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Employee>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create employee");
    }

    public async Task<Employee> UpdateAsync(int id, Employee employee, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"/employee/{id}", employee, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Employee>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update employee");
    }
}

public sealed class Employee
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("lastName")] public string? LastName { get; set; }
    [JsonPropertyName("employeeNumber")] public string? EmployeeNumber { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phoneNumberMobile")] public string? PhoneNumberMobile { get; set; }
    [JsonPropertyName("dateOfBirth")] public string? DateOfBirth { get; set; }
    [JsonPropertyName("department")] public IdRef? Department { get; set; }
}

public sealed class EmployeeCreate
{
    [JsonPropertyName("firstName")] public required string FirstName { get; set; }
    [JsonPropertyName("lastName")] public required string LastName { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("employeeNumber")] public string? EmployeeNumber { get; set; }
    [JsonPropertyName("dateOfBirth")] public string? DateOfBirth { get; set; }
}
