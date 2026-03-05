using System.CommandLine;
using Spectre.Console;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var cmd = new Command("config", "Manage CLI configuration");
        cmd.AddCommand(CreateSetCommand());
        cmd.AddCommand(CreateShowCommand());
        cmd.AddCommand(CreateUnsetCommand());
        return cmd;
    }

    private static Command CreateSetCommand()
    {
        var consumerToken = new Option<string?>("--consumer-token", "Tripletex consumer token");
        var employeeToken = new Option<string?>("--employee-token", "Tripletex employee token");
        var environment = new Option<string?>("--environment", "API environment (test or production)");

        var cmd = new Command("set", "Set configuration values")
        {
            consumerToken, employeeToken, environment
        };

        cmd.SetHandler((ct, et, env) =>
        {
            var config = ConfigStore.Load();

            if (ct is not null) config.ConsumerToken = ct;
            if (et is not null) config.EmployeeToken = et;
            if (env is not null) config.Environment = env;

            ConfigStore.Save(config);
            AnsiConsole.MarkupLine("[green]Configuration saved.[/]");
        }, consumerToken, employeeToken, environment);

        return cmd;
    }

    private static Command CreateShowCommand()
    {
        var cmd = new Command("show", "Show current configuration");

        cmd.SetHandler(() =>
        {
            var config = ConfigStore.Load();
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Setting");
            table.AddColumn("Value");

            table.AddRow("Consumer Token", Mask(config.ConsumerToken));
            table.AddRow("Employee Token", Mask(config.EmployeeToken));
            table.AddRow("Environment", config.Environment ?? "production");
            table.AddRow("Default Employee", config.DefaultEmployeeId.HasValue
                ? $"{config.DefaultEmployeeName} (ID: {config.DefaultEmployeeId})"
                : "[dim]not set[/]");
            table.AddRow("Default Project", config.DefaultProjectId.HasValue
                ? $"{config.DefaultProjectName} (ID: {config.DefaultProjectId})"
                : "[dim]not set[/]");
            table.AddRow("Default Activity", config.DefaultActivityId.HasValue
                ? $"{config.DefaultActivityName} (ID: {config.DefaultActivityId})"
                : "[dim]not set[/]");

            AnsiConsole.Write(table);
        });

        return cmd;
    }

    private static Command CreateUnsetCommand()
    {
        var employee = new Option<bool>("--employee", "Clear default employee");
        var project = new Option<bool>("--project", "Clear default project");
        var activity = new Option<bool>("--activity", "Clear default activity");
        var all = new Option<bool>("--all", "Clear all defaults");

        var cmd = new Command("unset", "Clear default employee, project, or activity")
        {
            employee, project, activity, all
        };

        cmd.SetHandler((emp, proj, act, clearAll) =>
        {
            var config = ConfigStore.Load();
            var changed = false;

            if (emp || clearAll)
            {
                config.DefaultEmployeeId = null;
                config.DefaultEmployeeName = null;
                changed = true;
            }
            if (proj || clearAll)
            {
                config.DefaultProjectId = null;
                config.DefaultProjectName = null;
                changed = true;
            }
            if (act || clearAll)
            {
                config.DefaultActivityId = null;
                config.DefaultActivityName = null;
                changed = true;
            }

            if (!changed)
            {
                AnsiConsole.MarkupLine("[yellow]Specify --employee, --project, --activity, or --all.[/]");
                return;
            }

            ConfigStore.Save(config);
            AnsiConsole.MarkupLine("[green]Defaults cleared.[/]");
        }, employee, project, activity, all);

        return cmd;
    }

    private static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "[dim]not set[/]";
        return value.Length > 8
            ? value[..4] + new string('*', value.Length - 8) + value[^4..]
            : new string('*', value.Length);
    }
}
