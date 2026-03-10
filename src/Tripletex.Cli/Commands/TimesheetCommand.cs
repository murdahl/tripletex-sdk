using System.CommandLine;
using Spectre.Console;
using Tripletex.Api;
using Tripletex.Api.Models;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class TimesheetCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("timesheet", "Manage timesheet entries");
        cmd.AddCommand(CreateLogCommand(jsonOption));
        cmd.AddCommand(CreateLogWeekCommand(jsonOption));
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateRecentCommand(jsonOption));
        cmd.AddCommand(CreateTotalHoursCommand(jsonOption));
        cmd.AddCommand(CreateDeleteCommand());
        cmd.AddCommand(CreateApproveWeekCommand());
        return cmd;
    }

    private enum LogStep { Employee, Project, Activity, Hours, Date, Comment, Confirm }

    internal const string BackSentinel = "← Back";

    private static Command CreateLogCommand(Option<bool> jsonOption)
    {
        var hours = new Argument<decimal?>("hours") { Arity = ArgumentArity.ZeroOrOne, Description = "Number of hours to log" };
        var date = new Option<string?>("--date", "Date (yyyy-MM-dd), defaults to today");
        var comment = new Option<string?>("--comment", "Comment for the entry");
        var projectId = new Option<int?>("--project-id", "Project ID (overrides default)");
        var activityId = new Option<int?>("--activity-id", "Activity ID (overrides default)");
        var employeeId = new Option<int?>("--employee-id", "Employee ID (overrides default)");

        var cmd = new Command("log", "Log hours (interactive if no arguments given)") { hours, date, comment, projectId, activityId, employeeId };

        cmd.SetHandler(async (ctx) =>
        {
            var h = ctx.ParseResult.GetValueForArgument(hours);
            var d = ctx.ParseResult.GetValueForOption(date);
            var c = ctx.ParseResult.GetValueForOption(comment);
            var pid = ctx.ParseResult.GetValueForOption(projectId);
            var aid = ctx.ParseResult.GetValueForOption(activityId);
            var eid = ctx.ParseResult.GetValueForOption(employeeId);
            var json = ctx.ParseResult.GetValueForOption(jsonOption);

            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            int? resolvedEmployee = eid ?? config.DefaultEmployeeId;
            int? resolvedProject = pid ?? config.DefaultProjectId;
            int? resolvedActivity = aid ?? config.DefaultActivityId;
            decimal? resolvedHours = h;
            DateOnly? resolvedDate = d is not null ? DateOnly.Parse(d) : null;
            string? resolvedComment = c;

            string? employeeName = config.DefaultEmployeeName;
            string? projectName = config.DefaultProjectName;
            string? activityName = config.DefaultActivityName;

            var step = resolvedEmployee is null ? LogStep.Employee
                     : resolvedProject is null ? LogStep.Project
                     : resolvedActivity is null ? LogStep.Activity
                     : resolvedHours is null ? LogStep.Hours
                     : resolvedDate is null ? LogStep.Date
                     : resolvedComment is null ? LogStep.Comment
                     : LogStep.Confirm;

            var firstStep = step;

            if (step != LogStep.Confirm && Console.IsInputRedirected)
                throw new InvalidOperationException(
                    "Cannot run interactive timesheet log with piped stdin. Provide all options via arguments: --employee-id, --project-id, --activity-id, hours, --date, --comment.");

            while (step != LogStep.Confirm || true)
            {
                switch (step)
                {
                    case LogStep.Employee:
                    {
                        var result = await PromptEmployeeAsync(client, config, canGoBack: false);
                        if (result is null) return;
                        resolvedEmployee = result.Value.id;
                        employeeName = result.Value.name;
                        step = LogStep.Project;
                        break;
                    }
                    case LogStep.Project:
                    {
                        var result = await PromptProjectWithBackAsync(client, config, canGoBack: true);
                        if (result is { isBack: true }) { step = LogStep.Employee; break; }
                        if (result is null) return;
                        resolvedProject = result.Value.id;
                        projectName = result.Value.name;
                        resolvedActivity = null;
                        activityName = null;
                        step = LogStep.Activity;
                        break;
                    }
                    case LogStep.Activity:
                    {
                        var result = await PromptActivityWithBackAsync(client, config, resolvedProject!.Value, canGoBack: true);
                        if (result is { isBack: true }) { step = LogStep.Project; break; }
                        if (result is null) return;
                        resolvedActivity = result.Value.id;
                        activityName = result.Value.name;
                        step = LogStep.Hours;
                        break;
                    }
                    case LogStep.Hours:
                    {
                        resolvedHours ??= AnsiConsole.Prompt(
                            new TextPrompt<decimal>("Hours:")
                                .Validate(v => v > 0 ? ValidationResult.Success() : ValidationResult.Error("Must be > 0")));
                        step = LogStep.Date;
                        break;
                    }
                    case LogStep.Date:
                    {
                        resolvedDate ??= AnsiConsole.Prompt(
                            new TextPrompt<DateOnly>("Date:")
                                .DefaultValue(DateOnly.FromDateTime(DateTime.Today)));
                        step = LogStep.Comment;
                        break;
                    }
                    case LogStep.Comment:
                    {
                        resolvedComment ??= AnsiConsole.Prompt(
                            new TextPrompt<string>("Comment:")
                                .AllowEmpty());
                        if (string.IsNullOrWhiteSpace(resolvedComment)) resolvedComment = null;
                        step = LogStep.Confirm;
                        break;
                    }
                    case LogStep.Confirm:
                    {
                        AnsiConsole.MarkupLine($"[bold]Summary:[/]");
                        if (employeeName is not null)
                            AnsiConsole.MarkupLine($"  Employee: [cyan]{Markup.Escape(employeeName)}[/]");
                        AnsiConsole.MarkupLine($"  Project:  [cyan]{Markup.Escape(projectName ?? resolvedProject.ToString()!)}[/]");
                        AnsiConsole.MarkupLine($"  Activity: [cyan]{Markup.Escape(activityName ?? resolvedActivity.ToString()!)}[/]");
                        AnsiConsole.MarkupLine($"  Hours:    [cyan]{resolvedHours}[/]");
                        AnsiConsole.MarkupLine($"  Date:     [cyan]{resolvedDate:yyyy-MM-dd}[/]");
                        if (resolvedComment is not null)
                            AnsiConsole.MarkupLine($"  Comment:  [cyan]{Markup.Escape(resolvedComment)}[/]");

                        if (!AnsiConsole.Confirm("Submit?", defaultValue: true))
                        {
                            step = firstStep;
                            resolvedHours = h;
                            resolvedDate = d is not null ? DateOnly.Parse(d) : null;
                            resolvedComment = c;
                            break;
                        }

                        TimesheetEntry entry;
                        try
                        {
                            entry = await client.Timesheet.LogHoursAsync(
                                resolvedActivity!.Value, resolvedProject!.Value, resolvedDate!.Value,
                                resolvedHours!.Value, resolvedComment, resolvedEmployee);
                        }
                        catch (TripletexApiException ex) when (ex.StatusCode == 409)
                        {
                            var existing = await client.Timesheet.SearchAsync(new TimesheetSearchOptions
                            {
                                EmployeeId = resolvedEmployee,
                                ProjectId = resolvedProject,
                                ActivityId = resolvedActivity,
                                DateFrom = resolvedDate,
                                DateTo = resolvedDate!.Value.AddDays(1),
                            });

                            var match = existing.Values?.FirstOrDefault();
                            if (match is null)
                            {
                                AnsiConsole.MarkupLine("[red]Conflict: hours already registered but could not find existing entry.[/]");
                                return;
                            }

                            AnsiConsole.MarkupLine($"[yellow]Already registered on {resolvedDate:yyyy-MM-dd}:[/]");
                            AnsiConsole.MarkupLine($"  Hours:   [cyan]{match.Hours}[/]");
                            if (!string.IsNullOrWhiteSpace(match.Comment))
                                AnsiConsole.MarkupLine($"  Comment: [cyan]{Markup.Escape(match.Comment)}[/]");

                            if (!AnsiConsole.Confirm($"Overwrite with [cyan]{resolvedHours}h[/]?", defaultValue: false))
                                return;

                            entry = await client.Timesheet.UpdateAsync(match.Id, new TimesheetEntryUpdate
                            {
                                Id = match.Id,
                                Version = match.Version,
                                Activity = new IdRef { Id = resolvedActivity!.Value },
                                Project = new IdRef { Id = resolvedProject!.Value },
                                Date = resolvedDate!.Value.ToString("yyyy-MM-dd"),
                                Hours = resolvedHours!.Value,
                                Comment = resolvedComment ?? "",
                                Employee = resolvedEmployee.HasValue ? new IdRef { Id = resolvedEmployee.Value } : null,
                            });
                        }

                        if (json)
                        {
                            OutputFormatter.Print(entry, true);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine(
                                $"[green]Logged {resolvedHours}h on {resolvedDate:yyyy-MM-dd} — {Markup.Escape(projectName ?? resolvedProject.ToString()!)} / {Markup.Escape(activityName ?? resolvedActivity.ToString()!)}[/]");
                        }
                        return;
                    }
                }
            }
        });

        return cmd;
    }

    internal const string FilterSentinelLabel = "[blue]Filter...[/]";

    internal static T? FilterableSelect<T>(
        string title,
        IReadOnlyList<T> items,
        Func<T, string> searchText,
        Func<T, string> display,
        T? backSentinel = default,
        T? filterSentinel = default,
        int pageSize = 15) where T : class
    {
        while (true)
        {
            var choices = new List<T>(items);
            if (filterSentinel is not null && items.Count > pageSize)
                choices.Insert(backSentinel is not null ? 1 : 0, filterSentinel);

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title(title)
                    .PageSize(pageSize)
                    .UseConverter(i =>
                        i == backSentinel ? BackSentinel :
                        i == filterSentinel ? FilterSentinelLabel :
                        display(i))
                    .AddChoices(choices));

            if (selected != filterSentinel)
                return selected;

            var filter = AnsiConsole.Prompt(
                new TextPrompt<string>("Filter:")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(filter))
                continue;

            var filtered = items
                .Where(i => i == backSentinel || searchText(i).Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0 || (filtered.Count == 1 && filtered[0] == backSentinel))
            {
                AnsiConsole.MarkupLine($"[yellow]No matches for \"{Markup.Escape(filter)}\". Showing all.[/]");
                continue;
            }

            var result = AnsiConsole.Prompt(
                new SelectionPrompt<T>()
                    .Title($"{title} [dim](filtered: \"{Markup.Escape(filter)}\")[/]")
                    .PageSize(pageSize)
                    .UseConverter(i => i == backSentinel ? BackSentinel : display(i))
                    .AddChoices(filtered));

            return result;
        }
    }

    internal static async Task<(int id, string name)?> PromptEmployeeAsync(
        TripletexClient client, CliConfig config, bool canGoBack)
    {
        AnsiConsole.MarkupLine("[dim]Fetching employees...[/]");
        var result = await client.Employee.SearchAsync();
        var employees = result.Values ?? [];

        if (employees.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No employees found.[/]");
            return null;
        }

        var sorted = employees.OrderBy(e => $"{e.FirstName} {e.LastName}").ToList();
        var backSentinel = canGoBack ? new Employee { Id = -1, FirstName = BackSentinel } : null;
        if (backSentinel is not null) sorted.Insert(0, backSentinel);

        var selected = FilterableSelect(
            "Select employee",
            sorted,
            e => $"{e.FirstName} {e.LastName}",
            e => $"{e.FirstName} {e.LastName} [dim]ID: {e.Id}[/]",
            backSentinel,
            filterSentinel: new Employee { Id = -2 });

        if (selected is null || selected == backSentinel)
            return null;

        var emp = selected;
        var name = $"{emp.FirstName} {emp.LastName}";

        if (AnsiConsole.Confirm("Save as default employee?", defaultValue: false))
        {
            config.DefaultEmployeeId = emp.Id;
            config.DefaultEmployeeName = name;
            ConfigStore.Save(config);
            AnsiConsole.MarkupLine($"[green]Saved default employee: {Markup.Escape(name)} (ID: {emp.Id})[/]");
        }

        return (emp.Id, name);
    }

    internal static async Task<(int id, string? name, bool isBack)?> PromptProjectWithBackAsync(
        TripletexClient client, CliConfig config, bool canGoBack)
    {
        AnsiConsole.MarkupLine("[dim]No default project set. Fetching projects...[/]");
        var result = await client.Project.SearchAsync();
        var projects = result.Values ?? [];

        if (projects.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No projects found.[/]");
            return null;
        }

        var sorted = projects.OrderBy(p => p.Name).ToList();
        var backSentinel = canGoBack ? new Project { Id = -1, Name = BackSentinel } : null;
        if (backSentinel is not null) sorted.Insert(0, backSentinel);

        var selected = FilterableSelect(
            "Select project",
            sorted,
            p => $"{p.Name} {p.Number}",
            p => $"{p.Name} ({p.Number ?? "no number"}) [dim]ID: {p.Id}[/]",
            backSentinel,
            filterSentinel: new Project { Id = -2 });

        if (selected is null || selected == backSentinel)
            return (0, null, true);

        var project = selected;

        if (AnsiConsole.Confirm("Save as default project?", defaultValue: false))
        {
            config.DefaultProjectId = project.Id;
            config.DefaultProjectName = project.Name;
            ConfigStore.Save(config);
            AnsiConsole.MarkupLine($"[green]Saved default project: {Markup.Escape(project.Name ?? "")} (ID: {project.Id})[/]");
        }

        return (project.Id, project.Name, false);
    }

    private static async Task<(int id, string? name, bool isBack)?> PromptActivityWithBackAsync(
        TripletexClient client, CliConfig config, int projectId, bool canGoBack)
    {
        AnsiConsole.MarkupLine("[dim]Fetching activities for project...[/]");
        var project = await client.Project.GetAsync(projectId, fields: "projectActivities(activity(*))");
        var projectActivities = project.ProjectActivities?
            .Where(pa => !pa.IsClosed)
            .Select(pa => new Activity
            {
                Id = pa.Activity?.Id ?? pa.Id,
                Name = pa.Activity?.Name,
                DisplayName = pa.Activity?.DisplayName,
            })
            .ToList() ?? [];

        if (projectActivities.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No activities found for this project.[/]");
            return null;
        }

        var activities = projectActivities;

        var sorted = activities.OrderBy(a => a.DisplayName ?? a.Name ?? "").ToList();
        var backSentinel = canGoBack ? new Activity { Id = -1, Name = BackSentinel } : null;
        if (backSentinel is not null) sorted.Insert(0, backSentinel);

        var selected = FilterableSelect(
            "Select activity",
            sorted,
            a => a.DisplayName ?? a.Name ?? "",
            a => $"{a.DisplayName ?? a.Name ?? "Unnamed"} [dim]ID: {a.Id}[/]",
            backSentinel,
            filterSentinel: new Activity { Id = -2 });

        if (selected is null || selected == backSentinel)
            return (0, null, true);

        var activity = selected;
        var activityName = activity.DisplayName ?? activity.Name ?? $"Activity {activity.Id}";

        if (AnsiConsole.Confirm("Save as default activity?", defaultValue: false))
        {
            config.DefaultActivityId = activity.Id;
            config.DefaultActivityName = activityName;
            ConfigStore.Save(config);
            AnsiConsole.MarkupLine($"[green]Saved default activity: {Markup.Escape(activityName)} (ID: {activity.Id})[/]");
        }

        return (activity.Id, activityName, false);
    }

    internal static async Task<int?> PromptProjectAsync(TripletexClient client, CliConfig config)
    {
        var result = await PromptProjectWithBackAsync(client, config, canGoBack: false);
        return result?.id;
    }

    internal static async Task<int?> PromptActivityAsync(TripletexClient client, CliConfig config, int projectId)
    {
        var result = await PromptActivityWithBackAsync(client, config, projectId, canGoBack: false);
        return result?.id;
    }

    private static Command CreateLogWeekCommand(Option<bool> jsonOption)
    {
        var weekStart = new Argument<string>("week-start", "Monday of the week (yyyy-MM-dd)");
        var mon = new Argument<decimal>("mon", "Hours for Monday");
        var tue = new Argument<decimal>("tue", "Hours for Tuesday");
        var wed = new Argument<decimal>("wed", "Hours for Wednesday");
        var thu = new Argument<decimal>("thu", "Hours for Thursday");
        var fri = new Argument<decimal>("fri", "Hours for Friday");
        var sat = new Option<decimal>("--sat", () => 0, "Hours for Saturday");
        var sun = new Option<decimal>("--sun", () => 0, "Hours for Sunday");
        var comment = new Option<string?>("--comment", "Comment for all entries");
        var projectId = new Option<int?>("--project-id", "Project ID (overrides default)");
        var activityId = new Option<int?>("--activity-id", "Activity ID (overrides default)");

        var cmd = new Command("log-week", "Log hours for a full week")
        {
            weekStart, mon, tue, wed, thu, fri, sat, sun, comment, projectId, activityId
        };

        cmd.SetHandler(async (ctx) =>
        {
            var ws = DateOnly.Parse(ctx.ParseResult.GetValueForArgument(weekStart));
            var hoursPerDay = new[]
            {
                ctx.ParseResult.GetValueForArgument(mon),
                ctx.ParseResult.GetValueForArgument(tue),
                ctx.ParseResult.GetValueForArgument(wed),
                ctx.ParseResult.GetValueForArgument(thu),
                ctx.ParseResult.GetValueForArgument(fri),
                ctx.ParseResult.GetValueForOption(sat),
                ctx.ParseResult.GetValueForOption(sun),
            };
            var c = ctx.ParseResult.GetValueForOption(comment);
            var pid = ctx.ParseResult.GetValueForOption(projectId);
            var aid = ctx.ParseResult.GetValueForOption(activityId);
            var json = ctx.ParseResult.GetValueForOption(jsonOption);

            var config = ConfigStore.Load();
            var resolvedProject = pid ?? config.DefaultProjectId
                ?? throw new InvalidOperationException("No project specified. Use --project-id or 'project select'.");
            var resolvedActivity = aid ?? config.DefaultActivityId
                ?? throw new InvalidOperationException("No activity specified. Use --activity-id or 'activity select'.");

            using var client = ClientFactory.Create(config);
            var result = await client.Timesheet.LogWeekAsync(
                resolvedActivity, resolvedProject, ws, hoursPerDay, c);

            var entries = result.Values ?? [];

            if (json)
            {
                OutputFormatter.PrintList<TimesheetEntry>(entries, true);
            }
            else
            {
                var total = hoursPerDay.Sum();
                AnsiConsole.MarkupLine(
                    $"[green]Logged {entries.Count} entries ({total}h total) for week starting {ws:yyyy-MM-dd}[/]");
            }
        });

        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int?>("id") { Arity = ArgumentArity.ZeroOrOne, Description = "Timesheet entry ID" };
        var cmd = new Command("get", "Get a timesheet entry by ID") { id };

        cmd.SetHandler(async (entryId, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            await OutputFormatter.FetchAndPrint(OutputFormatter.ResolveIds(entryId), id => client.Timesheet.GetAsync(id), json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var fromDate = new Option<string?>("--from-date", "Start date (yyyy-MM-dd)");
        var toDate = new Option<string?>("--to-date", "End date (yyyy-MM-dd)");
        var employeeId = new Option<int?>("--employee-id", "Filter by employee ID");
        var projectId = new Option<int?>("--project-id", "Filter by project ID");

        var cmd = new Command("list", "List timesheet entries") { fromDate, toDate, employeeId, projectId };

        cmd.SetHandler(async (fd, td, eid, pid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            var options = new TimesheetSearchOptions
            {
                DateFrom = fd is not null ? DateOnly.Parse(fd) : null,
                DateTo = td is not null ? DateOnly.Parse(td) : null,
                EmployeeId = eid,
                ProjectId = pid
            };

            var result = await client.Timesheet.SearchAsync(options);
            OutputFormatter.PrintList<TimesheetEntry>(result.Values ?? [], json);
        }, fromDate, toDate, employeeId, projectId, jsonOption);

        return cmd;
    }

    private static Command CreateRecentCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("recent", "Show recent timesheet entries");

        cmd.SetHandler(async (json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Timesheet.GetRecentAsync();
            OutputFormatter.PrintList<TimesheetEntry>(result.Values ?? [], json);
        }, jsonOption);

        return cmd;
    }

    private static Command CreateTotalHoursCommand(Option<bool> jsonOption)
    {
        var employeeId = new Argument<int>("employee-id", "Employee ID");
        var fromDate = new Argument<string>("from-date", "Start date (yyyy-MM-dd)");
        var toDate = new Argument<string>("to-date", "End date (yyyy-MM-dd)");

        var cmd = new Command("total-hours", "Get total hours for a period") { employeeId, fromDate, toDate };

        cmd.SetHandler(async (eid, fd, td, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Timesheet.GetTotalHoursAsync(
                eid, DateOnly.Parse(fd), DateOnly.Parse(td));
            OutputFormatter.Print(result, json);
        }, employeeId, fromDate, toDate, jsonOption);

        return cmd;
    }

    private static Command CreateDeleteCommand()
    {
        var id = new Argument<int>("id", "Timesheet entry ID to delete");
        var cmd = new Command("delete", "Delete a timesheet entry") { id };

        cmd.SetHandler(async (entryId) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            await client.Timesheet.DeleteAsync(entryId);
            AnsiConsole.MarkupLine($"[green]Deleted timesheet entry {entryId}.[/]");
        }, id);

        return cmd;
    }

    private static Command CreateApproveWeekCommand()
    {
        var employeeId = new Argument<int>("employee-id", "Employee ID");
        var weekStart = new Argument<string>("week-start", "Monday of the week (yyyy-MM-dd)");

        var cmd = new Command("approve-week", "Approve timesheet entries for a week") { employeeId, weekStart };

        cmd.SetHandler(async (eid, ws) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            await client.Timesheet.ApproveWeekAsync(eid, DateOnly.Parse(ws));
            AnsiConsole.MarkupLine($"[green]Approved week starting {ws} for employee {eid}.[/]");
        }, employeeId, weekStart);

        return cmd;
    }
}
