using System.CommandLine;
using Spectre.Console;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class ProjectCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("project", "Manage projects");
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateSearchCommand(jsonOption));
        cmd.AddCommand(CreateSelectCommand());
        cmd.AddCommand(CreateResetCommand());
        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("list", "List all projects");

        cmd.SetHandler(async (json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Project.SearchAsync();
            OutputFormatter.PrintList<Project>(result.Values ?? [], json);
        }, jsonOption);

        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int>("id", "Project ID");
        var cmd = new Command("get", "Get a project by ID") { id };

        cmd.SetHandler(async (projectId, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var project = await client.Project.GetAsync(projectId);
            OutputFormatter.Print(project, json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateSearchCommand(Option<bool> jsonOption)
    {
        var name = new Option<string?>("--name", "Search by project name");
        var cmd = new Command("search", "Search projects") { name };

        cmd.SetHandler(async (n, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Project.SearchAsync(name: n);
            OutputFormatter.PrintList<Project>(result.Values ?? [], json);
        }, name, jsonOption);

        return cmd;
    }

    private static Command CreateSelectCommand()
    {
        var cmd = new Command("select", "Interactively select a default project");

        cmd.SetHandler(async () =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            AnsiConsole.MarkupLine("[dim]Fetching projects...[/]");
            var result = await client.Project.SearchAsync();
            var projects = result.Values ?? [];

            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects found.[/]");
                return;
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<Project>()
                    .Title("Select default project:")
                    .PageSize(15)
                    .UseConverter(p => $"{p.Name} ({p.Number ?? "no number"}) [dim]ID: {p.Id}[/]")
                    .AddChoices(projects));

            config.DefaultProjectId = selected.Id;
            config.DefaultProjectName = selected.Name;
            ConfigStore.Save(config);

            AnsiConsole.MarkupLine($"[green]Saved default project: {Markup.Escape(selected.Name ?? "")} (ID: {selected.Id})[/]");
        });

        return cmd;
    }

    private static Command CreateResetCommand()
    {
        var cmd = new Command("reset", "Clear the default project");

        cmd.SetHandler(() =>
        {
            var config = ConfigStore.Load();
            config.DefaultProjectId = null;
            config.DefaultProjectName = null;
            ConfigStore.Save(config);
            AnsiConsole.MarkupLine("[green]Default project cleared.[/]");
        });

        return cmd;
    }
}
