using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class ExpenseOperations(HttpClient http)
{
    public async Task<TravelExpense> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"travelExpense/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<TravelExpense>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Expense {id} not found");
    }

    public async Task<ListResponse<TravelExpense>> SearchAsync(
        ExpenseSearchOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new();
        var url = BuildSearchUrl(options);

        var response = await http.GetFromJsonAsync<ListResponse<TravelExpense>>(url, ct);
        return response ?? new ListResponse<TravelExpense>();
    }

    public async IAsyncEnumerable<TravelExpense> ListAsync(
        ExpenseSearchOptions? options = null,
        string? fields = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        options ??= new();
        await foreach (var item in PaginationExtensions.PaginateAsync<TravelExpense>(
            (f, c, token) =>
            {
                var opts = options with { From = f, Count = c, Fields = fields };
                return http.GetAsync(BuildSearchUrl(opts), token);
            },
            pageSize, ct))
        {
            yield return item;
        }
    }

    public async Task<TravelExpense> CreateAsync(TravelExpenseCreate expense, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("travelExpense", expense, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TravelExpense>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create expense");
    }

    public async Task<TravelExpense> UpdateAsync(int id, TravelExpense expense, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"travelExpense/{id}", expense, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TravelExpense>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update expense");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"travelExpense/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TravelExpense> ConvertAsync(int id, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"travelExpense/{id}/convert", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TravelExpense>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to convert expense");
    }

    public async Task<ExpenseCost> GetCostAsync(int costId, string? fields = null, CancellationToken ct = default)
    {
        var url = $"travelExpense/cost/{costId}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<ExpenseCost>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Cost {costId} not found");
    }

    public async Task<ListResponse<ExpenseCost>> SearchCostsAsync(
        int? travelExpenseId = null,
        int? vatTypeId = null,
        int? currencyId = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (travelExpenseId.HasValue) parts.Add($"travelExpenseId={travelExpenseId}");
        if (vatTypeId.HasValue) parts.Add($"vatTypeId={vatTypeId}");
        if (currencyId.HasValue) parts.Add($"currencyId={currencyId}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "travelExpense/cost?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<ExpenseCost>>(url, ct);
        return response ?? new ListResponse<ExpenseCost>();
    }

    public async Task<ExpenseCost> CreateCostAsync(ExpenseCostCreate cost, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("travelExpense/cost", cost, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<ExpenseCost>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create cost");
    }

    public async Task<ExpenseCost> UpdateCostAsync(int costId, ExpenseCost cost, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"travelExpense/cost/{costId}", cost, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<ExpenseCost>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update cost");
    }

    public async Task DeleteCostAsync(int costId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"travelExpense/cost/{costId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ListResponse<ExpenseCostCategory>> SearchCostCategoriesAsync(
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string> { $"from={from}", $"count={count}" };
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "travelExpense/costCategory?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<ExpenseCostCategory>>(url, ct);
        return response ?? new ListResponse<ExpenseCostCategory>();
    }

    public async Task<ListResponse<ExpensePaymentType>> SearchPaymentTypesAsync(
        bool? showOnEmployeeExpenses = null,
        bool? showOnTravelExpenses = null,
        int from = 0,
        int count = 1000,
        string? fields = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (showOnEmployeeExpenses.HasValue) parts.Add($"showOnEmployeeExpenses={showOnEmployeeExpenses.Value.ToString().ToLowerInvariant()}");
        if (showOnTravelExpenses.HasValue) parts.Add($"showOnTravelExpenses={showOnTravelExpenses.Value.ToString().ToLowerInvariant()}");
        parts.Add($"from={from}");
        parts.Add($"count={count}");
        if (fields is not null) parts.Add($"fields={Uri.EscapeDataString(fields)}");

        var url = "travelExpense/paymentType?" + string.Join("&", parts);
        var response = await http.GetFromJsonAsync<ListResponse<ExpensePaymentType>>(url, ct);
        return response ?? new ListResponse<ExpensePaymentType>();
    }

    private static string BuildSearchUrl(ExpenseSearchOptions o)
    {
        var parts = new List<string>();
        if (o.EmployeeId is not null) parts.Add($"employeeId={o.EmployeeId}");
        if (o.DepartmentId is not null) parts.Add($"departmentId={o.DepartmentId}");
        if (o.ProjectId is not null) parts.Add($"projectId={o.ProjectId}");
        if (o.ProjectManagerId is not null) parts.Add($"projectManagerId={o.ProjectManagerId}");
        if (o.DepartureDateFrom.HasValue) parts.Add($"departureDateFrom={o.DepartureDateFrom:yyyy-MM-dd}");
        if (o.ReturnDateTo.HasValue) parts.Add($"returnDateTo={o.ReturnDateTo:yyyy-MM-dd}");
        if (o.State is not null) parts.Add($"state={o.State}");
        parts.Add($"from={o.From}");
        parts.Add($"count={o.Count}");
        if (o.Fields is not null) parts.Add($"fields={Uri.EscapeDataString(o.Fields)}");
        return "travelExpense?" + string.Join("&", parts);
    }
}

public sealed record ExpenseSearchOptions
{
    public string? EmployeeId { get; init; }
    public string? DepartmentId { get; init; }
    public string? ProjectId { get; init; }
    public string? ProjectManagerId { get; init; }
    public DateOnly? DepartureDateFrom { get; init; }
    public DateOnly? ReturnDateTo { get; init; }
    public string? State { get; init; }
    public int From { get; init; }
    public int Count { get; init; } = 1000;
    public string? Fields { get; init; }
}

public sealed class TravelExpense
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("employee")] public IdRef? Employee { get; set; }
    [JsonPropertyName("project")] public IdRef? Project { get; set; }
    [JsonPropertyName("department")] public IdRef? Department { get; set; }
    [JsonPropertyName("approvedBy")] public IdRef? ApprovedBy { get; set; }
    [JsonPropertyName("travelDetails")] public TravelDetails? TravelDetails { get; set; }
    [JsonPropertyName("isCompleted")] public bool IsCompleted { get; set; }
    [JsonPropertyName("isApproved")] public bool IsApproved { get; set; }
    [JsonPropertyName("isChargeable")] public bool IsChargeable { get; set; }
    [JsonPropertyName("isFixedInvoicedAmount")] public bool IsFixedInvoicedAmount { get; set; }
    [JsonPropertyName("isIncludeAttachedReceiptsWhenReinvoicing")] public bool IsIncludeAttachedReceiptsWhenReinvoicing { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("completedDate")] public string? CompletedDate { get; set; }
    [JsonPropertyName("approvedDate")] public string? ApprovedDate { get; set; }
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("paymentAmount")] public decimal PaymentAmount { get; set; }
    [JsonPropertyName("chargeableAmount")] public decimal ChargeableAmount { get; set; }
    [JsonPropertyName("travelAdvance")] public decimal TravelAdvance { get; set; }
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("type")] public int Type { get; set; }
    [JsonPropertyName("costs")] public List<ExpenseCost>? Costs { get; set; }
    [JsonPropertyName("attachmentCount")] public int AttachmentCount { get; set; }
    [JsonPropertyName("rejectedComment")] public string? RejectedComment { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
}

public sealed class TravelExpenseCreate
{
    [JsonPropertyName("employee")] public required IdRef Employee { get; set; }
    [JsonPropertyName("project")] public IdRef? Project { get; set; }
    [JsonPropertyName("department")] public IdRef? Department { get; set; }
    [JsonPropertyName("travelDetails")] public TravelDetails? TravelDetails { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("isChargeable")] public bool IsChargeable { get; set; }
    [JsonPropertyName("isFixedInvoicedAmount")] public bool IsFixedInvoicedAmount { get; set; }
    [JsonPropertyName("isIncludeAttachedReceiptsWhenReinvoicing")] public bool IsIncludeAttachedReceiptsWhenReinvoicing { get; set; }
    [JsonPropertyName("costs")] public List<ExpenseCostCreate>? Costs { get; set; }
}

public sealed class TravelDetails
{
    [JsonPropertyName("isForeignTravel")] public bool IsForeignTravel { get; set; }
    [JsonPropertyName("isDayTrip")] public bool IsDayTrip { get; set; }
    [JsonPropertyName("departureDate")] public string? DepartureDate { get; set; }
    [JsonPropertyName("returnDate")] public string? ReturnDate { get; set; }
    [JsonPropertyName("departureFrom")] public string? DepartureFrom { get; set; }
    [JsonPropertyName("destination")] public string? Destination { get; set; }
    [JsonPropertyName("departureTime")] public string? DepartureTime { get; set; }
    [JsonPropertyName("returnTime")] public string? ReturnTime { get; set; }
    [JsonPropertyName("purpose")] public string? Purpose { get; set; }
}

public sealed class ExpenseCost
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("travelExpense")] public IdRef? TravelExpense { get; set; }
    [JsonPropertyName("vatType")] public IdRef? VatType { get; set; }
    [JsonPropertyName("currency")] public IdRef? Currency { get; set; }
    [JsonPropertyName("costCategory")] public IdRef? CostCategory { get; set; }
    [JsonPropertyName("paymentType")] public IdRef? PaymentType { get; set; }
    [JsonPropertyName("category")] public string? Category { get; set; }
    [JsonPropertyName("comments")] public string? Comments { get; set; }
    [JsonPropertyName("rate")] public decimal Rate { get; set; }
    [JsonPropertyName("amountCurrencyIncVat")] public decimal AmountCurrencyIncVat { get; set; }
    [JsonPropertyName("amountNOKInclVAT")] public decimal AmountNokInclVat { get; set; }
    [JsonPropertyName("isPaidByEmployee")] public bool IsPaidByEmployee { get; set; }
    [JsonPropertyName("isChargeable")] public bool IsChargeable { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
}

public sealed class ExpenseCostCreate
{
    [JsonPropertyName("paymentType")] public required IdRef PaymentType { get; set; }
    [JsonPropertyName("costCategory")] public required IdRef CostCategory { get; set; }
    [JsonPropertyName("date")] public required string Date { get; set; }
    [JsonPropertyName("amountCurrencyIncVat")] public required decimal AmountCurrencyIncVat { get; set; }
    [JsonPropertyName("currency")] public IdRef? Currency { get; set; }
    [JsonPropertyName("vatType")] public IdRef? VatType { get; set; }
    [JsonPropertyName("comments")] public string? Comments { get; set; }
    [JsonPropertyName("isPaidByEmployee")] public bool IsPaidByEmployee { get; set; } = true;
    [JsonPropertyName("isChargeable")] public bool IsChargeable { get; set; }
    [JsonPropertyName("travelExpense")] public IdRef? TravelExpense { get; set; }
}

public sealed class ExpenseCostCategory
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("showOnTravelExpenses")] public bool ShowOnTravelExpenses { get; set; }
    [JsonPropertyName("showOnEmployeeExpenses")] public bool ShowOnEmployeeExpenses { get; set; }
    [JsonPropertyName("isInactive")] public bool IsInactive { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
}

public sealed class ExpensePaymentType
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("showOnTravelExpenses")] public bool ShowOnTravelExpenses { get; set; }
    [JsonPropertyName("showOnEmployeeExpenses")] public bool ShowOnEmployeeExpenses { get; set; }
    [JsonPropertyName("isInactive")] public bool IsInactive { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
}
