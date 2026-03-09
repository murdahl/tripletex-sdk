using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tripletex.Api.Models;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class TimesheetOperations(HttpClient http)
{
    /// <summary>Get a single timesheet entry by ID.</summary>
    public async Task<TimesheetEntry> GetAsync(int id, string? fields = null, CancellationToken ct = default)
    {
        var url = $"timesheet/entry/{id}";
        if (fields is not null) url += $"?fields={Uri.EscapeDataString(fields)}";

        var response = await http.GetFromJsonAsync<SingleResponse<TimesheetEntry>>(url, ct);
        return response?.Value ?? throw new InvalidOperationException($"Timesheet entry {id} not found");
    }

    /// <summary>Search/list timesheet entries with filters.</summary>
    public async Task<ListResponse<TimesheetEntry>> SearchAsync(
        TimesheetSearchOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new();
        var url = BuildSearchUrl(options);

        var response = await http.GetFromJsonAsync<ListResponse<TimesheetEntry>>(url, ct);
        return response ?? new ListResponse<TimesheetEntry>();
    }

    /// <summary>Stream all matching timesheet entries with auto-pagination.</summary>
    public async IAsyncEnumerable<TimesheetEntry> ListAsync(
        TimesheetSearchOptions? options = null,
        int pageSize = 1000,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        options ??= new();

        await foreach (var entry in PaginationExtensions.PaginateAsync<TimesheetEntry>(
            (from, count, token) =>
            {
                options.From = from;
                options.Count = count;
                var url = BuildSearchUrl(options);
                return http.GetAsync(url, token);
            },
            pageSize, ct))
        {
            yield return entry;
        }
    }

    /// <summary>Create a new timesheet entry (log hours).</summary>
    public async Task<TimesheetEntry> CreateAsync(TimesheetEntryCreate entry, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("timesheet/entry", entry, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TimesheetEntry>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to create timesheet entry");
    }

    /// <summary>Create multiple timesheet entries in a single call.</summary>
    public async Task<ListResponse<TimesheetEntry>> CreateBulkAsync(
        IEnumerable<TimesheetEntryCreate> entries, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("timesheet/entry/list", entries, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponse<TimesheetEntry>>(ct);
        return result ?? new ListResponse<TimesheetEntry>();
    }

    /// <summary>Update an existing timesheet entry.</summary>
    public async Task<TimesheetEntry> UpdateAsync(int id, TimesheetEntryUpdate entry, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"timesheet/entry/{id}", entry, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TimesheetEntry>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to update timesheet entry");
    }

    /// <summary>Update multiple timesheet entries in a single call.</summary>
    public async Task<ListResponse<TimesheetEntry>> UpdateBulkAsync(
        IEnumerable<TimesheetEntryUpdate> entries, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync("timesheet/entry/list", entries, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponse<TimesheetEntry>>(ct);
        return result ?? new ListResponse<TimesheetEntry>();
    }

    /// <summary>Delete a timesheet entry.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"timesheet/entry/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Get recent timesheet entries (last 30 days, newest first).</summary>
    public Task<ListResponse<TimesheetEntry>> GetRecentAsync(
        int? employeeId = null,
        int count = 25,
        string? fields = null,
        CancellationToken ct = default)
    {
        return SearchAsync(new TimesheetSearchOptions
        {
            EmployeeId = employeeId,
            DateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            DateTo = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Count = count,
            Sorting = "-date",
            Fields = fields,
        }, ct);
    }

    /// <summary>Get the total hours summary for a period.</summary>
    public async Task<TimesheetTotalHours> GetTotalHoursAsync(
        int employeeId,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken ct = default)
    {
        var url = $"timesheet/entry/totalHours?employeeId={employeeId}" +
                  $"&dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";

        var response = await http.GetFromJsonAsync<SingleResponse<TimesheetTotalHours>>(url, ct);
        return response?.Value ?? new TimesheetTotalHours();
    }

    /// <summary>Approve timesheet entries for a week (typically for managers).</summary>
    public async Task ApproveWeekAsync(
        int employeeId,
        DateOnly weekStartDate,
        CancellationToken ct = default)
    {
        var url = $"timesheet/week/:approve?employeeId={employeeId}&weekYear={weekStartDate:yyyy-MM-dd}";
        var response = await http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Unapprove timesheet entries for a week.</summary>
    public async Task UnapproveWeekAsync(
        int employeeId,
        DateOnly weekStartDate,
        CancellationToken ct = default)
    {
        var url = $"timesheet/week/:unapprove?employeeId={employeeId}&weekYear={weekStartDate:yyyy-MM-dd}";
        var response = await http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Approve a timesheet month by its ID.</summary>
    public async Task ApproveMonthAsync(int id, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"timesheet/month/:approve?id={id}", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Approve a timesheet month by employee and month.</summary>
    public async Task ApproveMonthAsync(
        int employeeId,
        string monthYear,
        CancellationToken ct = default)
    {
        var url = $"timesheet/month/:approve?employeeIds={employeeId}&monthYear={Uri.EscapeDataString(monthYear)}";
        var response = await http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Unapprove a timesheet month by its ID.</summary>
    public async Task UnapproveMonthAsync(int id, CancellationToken ct = default)
    {
        var response = await http.PutAsync($"timesheet/month/:unapprove?id={id}", null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Unapprove a timesheet month by employee and month.</summary>
    public async Task UnapproveMonthAsync(
        int employeeId,
        string monthYear,
        CancellationToken ct = default)
    {
        var url = $"timesheet/month/:unapprove?employeeIds={employeeId}&monthYear={Uri.EscapeDataString(monthYear)}";
        var response = await http.PutAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Get recently used activities for timesheet entries.</summary>
    public async Task<ListResponse<IdRef>> GetRecentActivitiesAsync(
        int? projectId = null,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (projectId.HasValue) parts.Add($"projectId={projectId}");
        if (employeeId.HasValue) parts.Add($"employeeId={employeeId}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";

        var response = await http.GetFromJsonAsync<ListResponse<IdRef>>(
            $"timesheet/entry/recentActivities{query}", ct);
        return response ?? new ListResponse<IdRef>();
    }

    /// <summary>Get projects with recent timesheet activity.</summary>
    public async Task<ListResponse<IdRef>> GetRecentProjectsAsync(
        int? employeeId = null,
        CancellationToken ct = default)
    {
        var url = "timesheet/entry/recentProjects";
        if (employeeId.HasValue) url += $"?employeeId={employeeId}";

        var response = await http.GetFromJsonAsync<ListResponse<IdRef>>(url, ct);
        return response ?? new ListResponse<IdRef>();
    }

    /// <summary>Get timesheet settings for an employee.</summary>
    public async Task<TimesheetSettings> GetSettingsAsync(
        int? employeeId = null,
        CancellationToken ct = default)
    {
        var url = "timesheet/settings";
        if (employeeId.HasValue) url += $"?employeeId={employeeId}";

        var response = await http.GetFromJsonAsync<SingleResponse<TimesheetSettings>>(url, ct);
        return response?.Value ?? new TimesheetSettings();
    }

    /// <summary>Log hours — convenience method that creates a timesheet entry with minimal parameters.</summary>
    public Task<TimesheetEntry> LogHoursAsync(
        int activityId,
        int projectId,
        DateOnly date,
        decimal hours,
        string? comment = null,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        return CreateAsync(new TimesheetEntryCreate
        {
            Activity = new IdRef { Id = activityId },
            Project = new IdRef { Id = projectId },
            Date = date.ToString("yyyy-MM-dd"),
            Hours = hours,
            Comment = comment ?? "",
            Employee = employeeId.HasValue ? new IdRef { Id = employeeId.Value } : null,
        }, ct);
    }

    /// <summary>Log hours for multiple days at once — e.g., fill a whole week.</summary>
    public Task<ListResponse<TimesheetEntry>> LogWeekAsync(
        int activityId,
        int projectId,
        DateOnly weekStart,
        decimal[] hoursPerDay,
        string? comment = null,
        int? employeeId = null,
        CancellationToken ct = default)
    {
        var entries = new List<TimesheetEntryCreate>();

        for (var i = 0; i < hoursPerDay.Length && i < 7; i++)
        {
            if (hoursPerDay[i] <= 0) continue;

            entries.Add(new TimesheetEntryCreate
            {
                Activity = new IdRef { Id = activityId },
                Project = new IdRef { Id = projectId },
                Date = weekStart.AddDays(i).ToString("yyyy-MM-dd"),
                Hours = hoursPerDay[i],
                Comment = comment ?? "",
                Employee = employeeId.HasValue ? new IdRef { Id = employeeId.Value } : null,
            });
        }

        return CreateBulkAsync(entries, ct);
    }

    private static string BuildSearchUrl(TimesheetSearchOptions options)
    {
        var parts = new List<string>();

        if (options.EmployeeId.HasValue) parts.Add($"employeeId={options.EmployeeId}");
        if (options.ProjectId.HasValue) parts.Add($"projectId={options.ProjectId}");
        if (options.ActivityId.HasValue) parts.Add($"activityId={options.ActivityId}");
        if (options.DateFrom.HasValue) parts.Add($"dateFrom={options.DateFrom:yyyy-MM-dd}");
        if (options.DateTo.HasValue) parts.Add($"dateTo={options.DateTo:yyyy-MM-dd}");
        if (options.From > 0) parts.Add($"from={options.From}");
        if (options.Count > 0) parts.Add($"count={options.Count}");
        if (options.Sorting is not null) parts.Add($"sorting={Uri.EscapeDataString(options.Sorting)}");
        if (options.Fields is not null) parts.Add($"fields={Uri.EscapeDataString(options.Fields)}");

        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        return $"timesheet/entry{query}";
    }
}

public sealed class TimesheetSearchOptions
{
    public int? EmployeeId { get; set; }
    public int? ProjectId { get; set; }
    public int? ActivityId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int From { get; set; }
    public int Count { get; set; } = 1000;
    public string? Sorting { get; set; }
    public string? Fields { get; set; }
}

public sealed class TimesheetEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("employee")]
    public IdRef? Employee { get; set; }

    [JsonPropertyName("project")]
    public IdRef? Project { get; set; }

    [JsonPropertyName("activity")]
    public IdRef? Activity { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("hours")]
    public decimal Hours { get; set; }

    [JsonPropertyName("chargeableHours")]
    public decimal ChargeableHours { get; set; }

    [JsonPropertyName("chargeable")]
    public bool Chargeable { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("approved")]
    public bool Approved { get; set; }

    [JsonPropertyName("approvedDate")]
    public string? ApprovedDate { get; set; }

    [JsonPropertyName("invoice")]
    public IdRef? Invoice { get; set; }

    [JsonPropertyName("hourlyRate")]
    public decimal HourlyRate { get; set; }

    [JsonPropertyName("hourlyCost")]
    public decimal HourlyCost { get; set; }
}

public sealed class TimesheetEntryCreate
{
    [JsonPropertyName("employee")]
    public IdRef? Employee { get; set; }

    [JsonPropertyName("project")]
    public IdRef? Project { get; set; }

    [JsonPropertyName("activity")]
    public IdRef? Activity { get; set; }

    [JsonPropertyName("date")]
    public required string Date { get; set; }

    [JsonPropertyName("hours")]
    public decimal Hours { get; set; }

    [JsonPropertyName("chargeableHours")]
    public decimal? ChargeableHours { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class TimesheetEntryUpdate
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("employee")]
    public IdRef? Employee { get; set; }

    [JsonPropertyName("project")]
    public IdRef? Project { get; set; }

    [JsonPropertyName("activity")]
    public IdRef? Activity { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("hours")]
    public decimal Hours { get; set; }

    [JsonPropertyName("chargeableHours")]
    public decimal? ChargeableHours { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public sealed class TimesheetTotalHours
{
    [JsonPropertyName("registered")]
    public decimal Registered { get; set; }

    [JsonPropertyName("paid")]
    public decimal Paid { get; set; }
}

public sealed class TimesheetSettings
{
    [JsonPropertyName("timeClock")]
    public bool TimeClock { get; set; }

    [JsonPropertyName("flexiTime")]
    public bool FlexiTime { get; set; }
}

public sealed class IdRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
