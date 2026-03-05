using System.CommandLine;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class InvoiceCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("invoice", "Manage invoices");
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int>("id", "Invoice ID");
        var cmd = new Command("get", "Get an invoice by ID") { id };

        cmd.SetHandler(async (iid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var invoice = await client.Invoice.GetAsync(iid);
            OutputFormatter.Print(invoice, json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var fromDate = new Option<string?>("--from-date", "Start date (yyyy-MM-dd)");
        var toDate = new Option<string?>("--to-date", "End date (yyyy-MM-dd)");
        var customerId = new Option<int?>("--customer-id", "Filter by customer ID");

        var cmd = new Command("list", "List invoices") { fromDate, toDate, customerId };

        cmd.SetHandler(async (fd, td, cid, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.Invoice.SearchAsync(
                invoiceDateFrom: fd is not null ? DateOnly.Parse(fd) : null,
                invoiceDateTo: td is not null ? DateOnly.Parse(td) : null,
                customerId: cid);
            OutputFormatter.PrintList<Invoice>(result.Values ?? [], json);
        }, fromDate, toDate, customerId, jsonOption);

        return cmd;
    }
}
