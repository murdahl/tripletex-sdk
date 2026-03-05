# Tripletex.Api

.NET 9 SDK for the [Tripletex API v2](https://tripletex.no/v2-docs/) (Norwegian accounting/ERP).

## Installation

```bash
dotnet add package Tripletex.Api
```

## Quick start

```csharp
using Tripletex.Api;

var client = new TripletexClient(new TripletexOptions
{
    ConsumerToken = "your-consumer-token",
    EmployeeToken = "your-employee-token",
    Environment = TripletexEnvironment.Test, // or Production
});
```

## Timesheet — log hours

```csharp
// Log 7.5 hours to a project/activity
var entry = await client.Timesheet.LogHoursAsync(
    activityId: 1234,
    projectId: 5678,
    date: new DateOnly(2026, 3, 5),
    hours: 7.5m,
    comment: "Feature development"
);

// Fill a whole week (Mon-Fri, 7.5h each)
var week = await client.Timesheet.LogWeekAsync(
    activityId: 1234,
    projectId: 5678,
    weekStart: new DateOnly(2026, 3, 2),
    hoursPerDay: [7.5m, 7.5m, 7.5m, 7.5m, 7.5m, 0, 0]
);
```

## Timesheet — query entries

```csharp
// Search with filters
var results = await client.Timesheet.SearchAsync(new TimesheetSearchOptions
{
    EmployeeId = 42,
    DateFrom = new DateOnly(2026, 3, 1),
    DateTo = new DateOnly(2026, 3, 31),
});

// Auto-paginate all results as IAsyncEnumerable
await foreach (var entry in client.Timesheet.ListAsync(new TimesheetSearchOptions
{
    ProjectId = 5678,
    DateFrom = new DateOnly(2026, 1, 1),
}))
{
    Console.WriteLine($"{entry.Date}: {entry.Hours}h - {entry.Comment}");
}

// Total hours for a period
var totals = await client.Timesheet.GetTotalHoursAsync(
    employeeId: 42,
    dateFrom: new DateOnly(2026, 3, 1),
    dateTo: new DateOnly(2026, 3, 31)
);
```

## Timesheet — approve/manage

```csharp
await client.Timesheet.ApproveMonthAsync(employeeId: 42, monthYear: "2026-03");
await client.Timesheet.UnapproveMonthAsync(employeeId: 42, monthYear: "2026-03");
```

## Other resources

```csharp
var employees = await client.Employee.SearchAsync(lastName: "Hansen");
var projects = await client.Project.SearchAsync(name: "Website");
var invoice = await client.Invoice.GetAsync(123);
var customers = await client.Customer.SearchAsync(name: "Acme");
```

## Dependency injection (ASP.NET Core)

```csharp
builder.Services.AddTripletex(options =>
{
    options.ConsumerToken = builder.Configuration["Tripletex:ConsumerToken"]!;
    options.EmployeeToken = builder.Configuration["Tripletex:EmployeeToken"]!;
    options.Environment = TripletexEnvironment.Production;
});
```

Then inject `TripletexClient` wherever needed.

## Features

- Session token management — auto-creates and refreshes tokens
- Rate limit handling — auto-retries on 429 with backoff
- Auto-pagination — `IAsyncEnumerable<T>` over all list endpoints
- Field selection — typed builder for the `fields` query param
- Error handling — Tripletex errors deserialized into `TripletexApiException`
- Path rewriting — handles Tripletex's `:action` and `>summary` URL conventions

## Configuration

| Option | Default | Description |
|---|---|---|
| `ConsumerToken` | *required* | Your Tripletex consumer token |
| `EmployeeToken` | *required* | Your Tripletex employee token |
| `Environment` | `Production` | `Production` or `Test` |
| `SessionLifetime` | 24 hours | How long session tokens last |
| `MaxRetries` | 3 | Max retries on rate limit (429) |
| `RetryBaseDelay` | 1 second | Base delay for exponential backoff |

## License

MIT
