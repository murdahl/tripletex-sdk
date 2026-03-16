using System.CommandLine;
using Spectre.Console;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class ActivityCommand
{
    public static Command Create()
    {
        var cmd = new Command("activity", "Manage activities");
        cmd.AddCommand(CreateSelectCommand());
        cmd.AddCommand(CreateResetCommand());
        return cmd;
    }

    private static Command CreateSelectCommand()
    {
        var projectId = new Option<int?>("--project-id", "Filter activities by project ID");
        var cmd = new Command("select", "Interactively select a default activity") { projectId };

        cmd.SetHandler(async (pid) =>
        {
            var config = ConfigStore.Load();
            var resolvedProjectId = pid ?? config.DefaultProjectId;

            using var client = ClientFactory.Create(config);

            List<Api.Operations.Activity> activities;

            if (resolvedProjectId is null or 0)
            {
                AnsiConsole.MarkupLine("[dim]Fetching internal (non-project) activities...[/]");

                var result = await client.Activity.SearchAsync(isProjectActivity: false, isInactive: false);
                activities = (result.Values ?? [])
                    .OrderBy(a => a.DisplayName ?? a.Name ?? "")
                    .ToList();
            }
            else
            {
                var projectLabel = config.DefaultProjectName ?? resolvedProjectId.ToString()!;
                AnsiConsole.MarkupLine($"[dim]Fetching activities for project {Markup.Escape(projectLabel)}...[/]");

                var project = await client.Project.GetAsync(resolvedProjectId.Value, fields: "projectActivities(activity(*))");
                activities = (project.ProjectActivities ?? [])
                    .Where(pa => !pa.IsClosed)
                    .Select(pa => new Api.Operations.Activity
                    {
                        Id = pa.Activity?.Id ?? pa.Id,
                        Name = pa.Activity?.Name,
                        DisplayName = pa.Activity?.DisplayName,
                    })
                    .OrderBy(a => a.DisplayName ?? a.Name ?? "")
                    .ToList();
            }

            if (activities.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No activities found for this project.[/]");
                return;
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<Api.Operations.Activity>()
                    .Title("Select default activity:")
                    .PageSize(15)
                    .UseConverter(a => $"{a.DisplayName ?? a.Name ?? "Unnamed"} [dim]ID: {a.Id}[/]")
                    .AddChoices(activities));

            var activityName = selected.DisplayName ?? selected.Name ?? $"Activity {selected.Id}";
            config.DefaultActivityId = selected.Id;
            config.DefaultActivityName = activityName;
            ConfigStore.Save(config);

            AnsiConsole.MarkupLine($"[green]Saved default activity: {Markup.Escape(activityName)} (ID: {selected.Id})[/]");
        }, projectId);

        return cmd;
    }

    private static Command CreateResetCommand()
    {
        var cmd = new Command("reset", "Clear the default activity");

        cmd.SetHandler(() =>
        {
            var config = ConfigStore.Load();
            config.DefaultActivityId = null;
            config.DefaultActivityName = null;
            ConfigStore.Save(config);
            AnsiConsole.MarkupLine("[green]Default activity cleared.[/]");
        });

        return cmd;
    }
}
