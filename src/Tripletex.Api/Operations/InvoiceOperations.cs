using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class InvoiceOperations(HttpClient http)
{
    public async Task<Invoice> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"/invoice/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<Invoice>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Invoice {id} not found");
    }

    public async Task<ListResponse<Invoice>> SearchAsync(
        DateOnly? invoiceDateFrom = null,
        DateOnly? invoiceDateTo = null,
        int? customerId = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (invoiceDateFrom.HasValue) parts.Add($"invoiceDateFrom={invoiceDateFrom:yyyy-MM-dd}");
        if (invoiceDateTo.HasValue) parts.Add($"invoiceDateTo={invoiceDateTo:yyyy-MM-dd}");
        if (customerId.HasValue) parts.Add($"customerId={customerId}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "/invoice?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<Invoice>>(url, ct);
        return response ?? new ListResponse<Invoice>();
    }

    public async IAsyncEnumerable<Invoice> ListAsync(
        DateOnly? invoiceDateFrom = null,
        DateOnly? invoiceDateTo = null,
        int? customerId = null,
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in PaginationExtensions.PaginateAsync<Invoice>(
            (f, c, token) =>
            {
                var parts = new List<string>();
                if (invoiceDateFrom.HasValue) parts.Add($"invoiceDateFrom={invoiceDateFrom:yyyy-MM-dd}");
                if (invoiceDateTo.HasValue) parts.Add($"invoiceDateTo={invoiceDateTo:yyyy-MM-dd}");
                if (customerId.HasValue) parts.Add($"customerId={customerId}");
                parts.Add($"from={f}");
                parts.Add($"count={c}");
                if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");
                return http.GetAsync("/invoice?" + string.Join("&", parts), token);
            },
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<Invoice> CreateAsync(InvoiceCreate invoice, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/invoice", invoice, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Invoice>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create invoice");
    }

    public async Task SendAsync(int id, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"/invoice/{id}/:send", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Invoice> CreateCreditNoteAsync(int id, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"/invoice/{id}/:createCreditNote", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<Invoice>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create credit note");
    }
}

public sealed class Invoice
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("invoiceNumber")] public int InvoiceNumber { get; set; }
    [JsonPropertyName("invoiceDate")] public string? InvoiceDate { get; set; }
    [JsonPropertyName("customer")] public IdRef? Customer { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("amountOutstanding")] public decimal AmountOutstanding { get; set; }
    [JsonPropertyName("currency")] public IdRef? Currency { get; set; }
    [JsonPropertyName("isCreditNote")] public bool IsCreditNote { get; set; }
    [JsonPropertyName("comment")] public string? Comment { get; set; }
}

public sealed class InvoiceCreate
{
    [JsonPropertyName("invoiceDate")] public required string InvoiceDate { get; set; }
    [JsonPropertyName("invoiceDueDate")] public required string InvoiceDueDate { get; set; }
    [JsonPropertyName("customer")] public required IdRef Customer { get; set; }
    [JsonPropertyName("orders")] public List<IdRef>? Orders { get; set; }
    [JsonPropertyName("comment")] public string? Comment { get; set; }
}
