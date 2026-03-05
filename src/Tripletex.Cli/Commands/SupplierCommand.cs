using System.CommandLine;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class SupplierCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("supplier", "Manage suppliers");
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateSearchCommand(jsonOption));
        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int>("id", "Supplier ID");
        var cmd = new Command("get", "Get a supplier by ID") { id };

        cmd.SetHandler(async (sid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var supplier = await client.Supplier.GetAsync(sid);
            OutputFormatter.Print(supplier, json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("list", "List all suppliers");

        cmd.SetHandler(async (json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Supplier.SearchAsync();
            OutputFormatter.PrintList<Supplier>(result.Values ?? [], json);
        }, jsonOption);

        return cmd;
    }

    private static Command CreateSearchCommand(Option<bool> jsonOption)
    {
        var name = new Option<string?>("--name", "Search by name");
        var cmd = new Command("search", "Search suppliers") { name };

        cmd.SetHandler(async (n, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Supplier.SearchAsync(name: n);
            OutputFormatter.PrintList<Supplier>(result.Values ?? [], json);
        }, name, jsonOption);

        return cmd;
    }
}
