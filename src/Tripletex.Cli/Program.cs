using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Spectre.Console;
using Tripletex.Api.Models;
using Tripletex.Cli.Commands;

var jsonOption = new Option<bool>("--json", "Output results as JSON");

var rootCommand = new RootCommand("Tripletex CLI — manage timesheets, projects, and more")
{
    jsonOption
};

rootCommand.AddCommand(ConfigCommand.Create());
rootCommand.AddCommand(TimesheetCommand.Create(jsonOption));
rootCommand.AddCommand(ProjectCommand.Create(jsonOption));
rootCommand.AddCommand(ActivityCommand.Create());
rootCommand.AddCommand(EmployeeCommand.Create(jsonOption));
rootCommand.AddCommand(CustomerCommand.Create(jsonOption));
rootCommand.AddCommand(InvoiceCommand.Create(jsonOption));
rootCommand.AddCommand(SupplierCommand.Create(jsonOption));
rootCommand.AddCommand(ExpenseCommand.Create(jsonOption));

var parser = new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .UseExceptionHandler((ex, ctx) =>
    {
        var inner = ex is TargetInvocationException { InnerException: { } innerEx } ? innerEx : ex;

        switch (inner)
        {
            case TripletexApiException apiEx:
                AnsiConsole.MarkupLine($"[red]API Error ({apiEx.StatusCode}): {Markup.Escape(apiEx.Message)}[/]");
                if (apiEx.DeveloperMessage is not null)
                    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(apiEx.DeveloperMessage)}[/]");
                foreach (var v in apiEx.ValidationMessages)
                    AnsiConsole.MarkupLine($"[yellow]  {Markup.Escape(v.Field ?? "")}: {Markup.Escape(v.Message ?? "")}[/]");
                break;

            case HttpRequestException httpEx:
                AnsiConsole.MarkupLine($"[red]Network error: {Markup.Escape(httpEx.Message)}[/]");
                break;

            case InvalidOperationException opEx when IsAuthError(opEx):
                AnsiConsole.MarkupLine("[red]Authentication failed — could not create a session token.[/]");
                AnsiConsole.MarkupLine("[dim]This usually means your consumer or employee token is invalid or expired.[/]");
                AnsiConsole.MarkupLine("[dim]Update tokens with: tripletex config set --consumer-token <t> --employee-token <t>[/]");
                break;

            case InvalidOperationException opEx:
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(opEx.Message)}[/]");
                break;

            case FormatException fmtEx:
                AnsiConsole.MarkupLine($"[red]Invalid format: {Markup.Escape(fmtEx.Message)}[/]");
                break;

            default:
                AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(inner.Message ?? "Unknown error")}[/]");
                break;
        }

        ctx.ExitCode = 1;
    })
    .Build();

return await parser.InvokeAsync(args);

static bool IsAuthError(Exception ex) =>
    ex.Message.Contains("Session token", StringComparison.OrdinalIgnoreCase)
    || ex.Message.Contains("encryptedId", StringComparison.OrdinalIgnoreCase)
    || ex.StackTrace?.Contains("SessionTokenProvider", StringComparison.Ordinal) == true;
