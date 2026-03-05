using System.CommandLine;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class CustomerCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("customer", "Manage customers");
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateSearchCommand(jsonOption));
        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int>("id", "Customer ID");
        var cmd = new Command("get", "Get a customer by ID") { id };

        cmd.SetHandler(async (cid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var customer = await client.Customer.GetAsync(cid);
            OutputFormatter.Print(customer, json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("list", "List all customers");

        cmd.SetHandler(async (json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Customer.SearchAsync();
            OutputFormatter.PrintList<Customer>(result.Values ?? [], json);
        }, jsonOption);

        return cmd;
    }

    private static Command CreateSearchCommand(Option<bool> jsonOption)
    {
        var name = new Option<string?>("--name", "Search by name");
        var cmd = new Command("search", "Search customers") { name };

        cmd.SetHandler(async (n, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Customer.SearchAsync(name: n);
            OutputFormatter.PrintList<Customer>(result.Values ?? [], json);
        }, name, jsonOption);

        return cmd;
    }
}
