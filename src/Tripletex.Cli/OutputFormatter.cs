using System.Reflection;
using System.Text.Json;
using Spectre.Console;
using Tripletex.Api.Operations;

namespace Tripletex.Cli;

public static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Print<T>(T item, bool json) where T : class
    {
        if (json || Console.IsOutputRedirected)
        {
            Console.WriteLine(JsonSerializer.Serialize(item, JsonOptions));
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Field");
        table.AddColumn("Value");

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(item);
            var display = FormatValue(value);
            table.AddRow(Markup.Escape(prop.Name), Markup.Escape(display));
        }

        AnsiConsole.Write(table);
    }

    public static void PrintList<T>(IReadOnlyList<T> items, bool json) where T : class
    {
        if (json || Console.IsOutputRedirected)
        {
            Console.WriteLine(JsonSerializer.Serialize(items, JsonOptions));
            return;
        }

        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found.[/]");
            return;
        }

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var table = new Table().Border(TableBorder.Rounded);

        foreach (var prop in props)
            table.AddColumn(prop.Name);

        foreach (var item in items)
        {
            var row = props.Select(p => Markup.Escape(FormatValue(p.GetValue(item)))).ToArray();
            table.AddRow(row);
        }

        AnsiConsole.Write(table);
    }

    public static async Task FetchAndPrint<T>(List<int> ids, Func<int, Task<T>> fetch, bool json) where T : class
    {
        if (ids.Count == 1)
        {
            Print(await fetch(ids[0]), json);
            return;
        }

        var items = new List<T>();
        foreach (var id in ids)
            items.Add(await fetch(id));
        PrintList<T>(items, json);
    }

    public static List<int> ResolveIds(int? arg) =>
        arg.HasValue ? [arg.Value] : StdinReader.TryReadIds()
            ?? throw new InvalidOperationException("No ID provided. Pass an ID argument or pipe IDs via stdin.");

    private static string FormatValue(object? value) => value switch
    {
        null => "-",
        IdRef idRef => idRef.Id.ToString(),
        _ => value.ToString() ?? "-"
    };
}
