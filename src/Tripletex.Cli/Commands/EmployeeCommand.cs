using System.CommandLine;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class EmployeeCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("employee", "Manage employees");
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateSearchCommand(jsonOption));
        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int?>("id") { Arity = ArgumentArity.ZeroOrOne, Description = "Employee ID" };
        var cmd = new Command("get", "Get an employee by ID") { id };

        cmd.SetHandler(async (eid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            await OutputFormatter.FetchAndPrint(OutputFormatter.ResolveIds(eid), id => client.Employee.GetAsync(id), json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("list", "List all employees");

        cmd.SetHandler(async (json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Employee.SearchAsync();
            OutputFormatter.PrintList<Employee>(result.Values ?? [], json);
        }, jsonOption);

        return cmd;
    }

    private static Command CreateSearchCommand(Option<bool> jsonOption)
    {
        var firstName = new Option<string?>("--first-name", "Search by first name");
        var lastName = new Option<string?>("--last-name", "Search by last name");
        var cmd = new Command("search", "Search employees") { firstName, lastName };

        cmd.SetHandler(async (fn, ln, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Employee.SearchAsync(firstName: fn, lastName: ln);
            OutputFormatter.PrintList<Employee>(result.Values ?? [], json);
        }, firstName, lastName, jsonOption);

        return cmd;
    }
}
