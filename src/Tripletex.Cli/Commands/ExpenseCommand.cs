using System.CommandLine;
using Spectre.Console;
using Tripletex.Api;
using Tripletex.Api.Operations;
using Tripletex.Cli.Configuration;

namespace Tripletex.Cli.Commands;

public static class ExpenseCommand
{
    public static Command Create(Option<bool> jsonOption)
    {
        var cmd = new Command("expense", "Manage expenses (utlegg)");
        cmd.AddCommand(CreateGetCommand(jsonOption));
        cmd.AddCommand(CreateListCommand(jsonOption));
        cmd.AddCommand(CreateCreateCommand(jsonOption));
        cmd.AddCommand(CreateAddCostCommand(jsonOption));
        cmd.AddCommand(CreateUploadCommand(jsonOption));
        return cmd;
    }

    private static Command CreateGetCommand(Option<bool> jsonOption)
    {
        var id = new Argument<int?>("id") { Arity = ArgumentArity.ZeroOrOne, Description = "Expense ID" };
        var cmd = new Command("get", "Get an expense by ID") { id };

        cmd.SetHandler(async (expenseId, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            await OutputFormatter.FetchAndPrint(OutputFormatter.ResolveIds(expenseId), id => client.Expense.GetAsync(id), json);
        }, id, jsonOption);

        return cmd;
    }

    private static Command CreateListCommand(Option<bool> jsonOption)
    {
        var employeeId = new Option<string?>("--employee-id", "Filter by employee ID");
        var projectId = new Option<string?>("--project-id", "Filter by project ID");
        var state = new Option<string?>("--state", "Filter by state (OPEN, APPROVED, etc.)");
        var fromDate = new Option<string?>("--from-date", "Departure date from (yyyy-MM-dd)");
        var toDate = new Option<string?>("--to-date", "Return date to (yyyy-MM-dd)");

        var cmd = new Command("list", "Search/list expenses") { employeeId, projectId, state, fromDate, toDate };

        cmd.SetHandler(async (eid, pid, st, fd, td, json) =>
        {
            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            var options = new ExpenseSearchOptions
            {
                EmployeeId = eid,
                ProjectId = pid,
                State = st,
                DepartureDateFrom = fd is not null ? DateOnly.Parse(fd) : null,
                ReturnDateTo = td is not null ? DateOnly.Parse(td) : null,
            };

            var result = await client.Expense.SearchAsync(options);
            OutputFormatter.PrintList(result.Values ?? [], json);
        }, employeeId, projectId, state, fromDate, toDate, jsonOption);

        return cmd;
    }

    private static Command CreateUploadCommand(Option<bool> jsonOption)
    {
        var expenseId = new Argument<int>("expense-id", "Expense ID");
        var file = new Argument<string>("file", "File path to upload");
        var cmd = new Command("upload", "Upload attachment to an expense") { expenseId, file };

        cmd.SetHandler(async (eid, filePath, json) =>
        {
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(filePath)}[/]");
                return;
            }

            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);
            var result = await client.ExpenseAttachment.UploadAsync(eid, filePath);

            if (json)
                OutputFormatter.Print(result, json);
            else
                AnsiConsole.MarkupLine($"[green]Uploaded {Markup.Escape(Path.GetFileName(filePath))} to expense {eid}. Attachments: {result.AttachmentCount}[/]");
        }, expenseId, file, jsonOption);

        return cmd;
    }

    private static Command CreateAddCostCommand(Option<bool> jsonOption)
    {
        var expenseId = new Argument<int>("expense-id", "Expense ID to add cost to");
        var cmd = new Command("add-cost", "Add a cost line to an existing expense") { expenseId };

        cmd.SetHandler(async (eid, json) =>
        {
            if (Console.IsInputRedirected)
                throw new InvalidOperationException(
                    "Cannot run interactive add-cost with piped stdin. This command requires interactive prompts.");

            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            var expense = await client.Expense.GetAsync(eid);
            bool isTravelExpense = expense.TravelDetails is not null;

            var cost = await PromptCostAsync(client, isTravelExpense);
            if (cost is null) return;

            cost.TravelExpense = new IdRef { Id = eid };
            var created = await client.Expense.CreateCostAsync(cost);

            if (json)
                OutputFormatter.Print(created, json);
            else
                AnsiConsole.MarkupLine($"[green]Added cost line (ID: {created.Id}) — {created.AmountCurrencyIncVat:N2} kr[/]");
        }, expenseId, jsonOption);

        return cmd;
    }

    private enum CreateStep { Type, Employee, Project, Title, TravelDetails, Costs, Attachment, Confirm }

    private static Command CreateCreateCommand(Option<bool> jsonOption)
    {
        var cmd = new Command("create", "Create an expense (interactive wizard)");

        cmd.SetHandler(async (json) =>
        {
            if (Console.IsInputRedirected)
                throw new InvalidOperationException(
                    "Cannot run interactive expense creation with piped stdin. This command requires interactive prompts.");

            var config = ConfigStore.Load();
            using var client = ClientFactory.Create(config);

            bool isTravelExpense = false;
            int? employeeId = null;
            string? employeeName = null;
            int? projectId = null;
            string? projectName = null;
            string? title = null;
            TravelDetails? travelDetails = null;
            var costs = new List<ExpenseCostCreate>();
            string? attachmentPath = null;

            var step = CreateStep.Type;

            while (true)
            {
                switch (step)
                {
                    case CreateStep.Type:
                    {
                        var choice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Expense type")
                                .AddChoices("Employee Expense", "Travel Expense"));
                        isTravelExpense = choice == "Travel Expense";
                        step = CreateStep.Employee;
                        break;
                    }
                    case CreateStep.Employee:
                    {
                        var result = await TimesheetCommand.PromptEmployeeAsync(client, config, canGoBack: true);
                        if (result is null) { step = CreateStep.Type; break; }
                        employeeId = result.Value.id;
                        employeeName = result.Value.name;
                        step = CreateStep.Project;
                        break;
                    }
                    case CreateStep.Project:
                    {
                        var skipChoice = new Project { Id = -3, Name = "Skip (no project)" };
                        AnsiConsole.MarkupLine("[dim]Fetching projects...[/]");
                        var projResult = await client.Project.SearchAsync();
                        var projects = (projResult.Values ?? []).OrderBy(p => p.Name).ToList();

                        var backSentinel = new Project { Id = -1, Name = TimesheetCommand.BackSentinel };
                        projects.Insert(0, backSentinel);
                        projects.Insert(1, skipChoice);

                        var selected = TimesheetCommand.FilterableSelect(
                            "Select project",
                            projects,
                            p => $"{p.Name} {p.Number}",
                            p => p == skipChoice ? "[dim]Skip (no project)[/]"
                                : $"{p.Name} ({p.Number ?? "no number"}) [dim]ID: {p.Id}[/]",
                            backSentinel,
                            filterSentinel: new Project { Id = -2 });

                        if (selected is null || selected == backSentinel)
                        {
                            step = CreateStep.Employee;
                            break;
                        }

                        if (selected == skipChoice)
                        {
                            projectId = null;
                            projectName = null;
                        }
                        else
                        {
                            projectId = selected.Id;
                            projectName = selected.Name;
                        }
                        step = CreateStep.Title;
                        break;
                    }
                    case CreateStep.Title:
                    {
                        title = AnsiConsole.Prompt(
                            new TextPrompt<string>("Expense title:")
                                .Validate(t => !string.IsNullOrWhiteSpace(t)
                                    ? ValidationResult.Success()
                                    : ValidationResult.Error("Title required")));
                        step = isTravelExpense ? CreateStep.TravelDetails : CreateStep.Costs;
                        break;
                    }
                    case CreateStep.TravelDetails:
                    {
                        travelDetails = new TravelDetails();

                        travelDetails.DepartureDate = AnsiConsole.Prompt(
                            new TextPrompt<string>("Departure date (yyyy-MM-dd):")
                                .DefaultValue(DateTime.Today.ToString("yyyy-MM-dd")));

                        travelDetails.ReturnDate = AnsiConsole.Prompt(
                            new TextPrompt<string>("Return date (yyyy-MM-dd):")
                                .DefaultValue(DateTime.Today.ToString("yyyy-MM-dd")));

                        travelDetails.DepartureFrom = AnsiConsole.Prompt(
                            new TextPrompt<string>("Departure from:").AllowEmpty());
                        if (string.IsNullOrWhiteSpace(travelDetails.DepartureFrom))
                            travelDetails.DepartureFrom = null;

                        travelDetails.Destination = AnsiConsole.Prompt(
                            new TextPrompt<string>("Destination:").AllowEmpty());
                        if (string.IsNullOrWhiteSpace(travelDetails.Destination))
                            travelDetails.Destination = null;

                        travelDetails.Purpose = AnsiConsole.Prompt(
                            new TextPrompt<string>("Purpose:").AllowEmpty());
                        if (string.IsNullOrWhiteSpace(travelDetails.Purpose))
                            travelDetails.Purpose = null;

                        travelDetails.IsDayTrip = AnsiConsole.Confirm("Day trip?", defaultValue: false);
                        travelDetails.IsForeignTravel = AnsiConsole.Confirm("Foreign travel?", defaultValue: false);

                        step = CreateStep.Costs;
                        break;
                    }
                    case CreateStep.Costs:
                    {
                        if (costs.Count == 0 || AnsiConsole.Confirm("Add a cost line?", defaultValue: costs.Count == 0))
                        {
                            var cost = await PromptCostAsync(client, isTravelExpense);
                            if (cost is not null)
                            {
                                costs.Add(cost);
                                if (AnsiConsole.Confirm("Add another cost line?", defaultValue: false))
                                    break;
                            }
                        }
                        step = CreateStep.Attachment;
                        break;
                    }
                    case CreateStep.Attachment:
                    {
                        var path = AnsiConsole.Prompt(
                            new TextPrompt<string>("Attachment file path (leave empty to skip):")
                                .AllowEmpty());

                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            if (!File.Exists(path))
                            {
                                AnsiConsole.MarkupLine($"[yellow]File not found: {Markup.Escape(path)}[/]");
                                break;
                            }
                            attachmentPath = path;
                        }
                        step = CreateStep.Confirm;
                        break;
                    }
                    case CreateStep.Confirm:
                    {
                        AnsiConsole.MarkupLine("[bold]Summary:[/]");
                        AnsiConsole.MarkupLine($"  Type:     [cyan]{(isTravelExpense ? "Travel Expense" : "Employee Expense")}[/]");
                        if (employeeName is not null)
                            AnsiConsole.MarkupLine($"  Employee: [cyan]{Markup.Escape(employeeName)}[/]");
                        AnsiConsole.MarkupLine($"  Project:  [cyan]{Markup.Escape(projectName ?? "(none)")}[/]");
                        AnsiConsole.MarkupLine($"  Title:    [cyan]{Markup.Escape(title ?? "")}[/]");

                        if (travelDetails is not null)
                        {
                            AnsiConsole.MarkupLine($"  Departure: [cyan]{travelDetails.DepartureDate}[/] from [cyan]{Markup.Escape(travelDetails.DepartureFrom ?? "-")}[/]");
                            AnsiConsole.MarkupLine($"  Return:    [cyan]{travelDetails.ReturnDate}[/] to [cyan]{Markup.Escape(travelDetails.Destination ?? "-")}[/]");
                            if (travelDetails.Purpose is not null)
                                AnsiConsole.MarkupLine($"  Purpose:   [cyan]{Markup.Escape(travelDetails.Purpose)}[/]");
                        }

                        AnsiConsole.MarkupLine($"  Costs:    [cyan]{costs.Count} line(s), total {costs.Sum(c => c.AmountCurrencyIncVat):N2} kr[/]");
                        if (attachmentPath is not null)
                            AnsiConsole.MarkupLine($"  Attachment: [cyan]{Markup.Escape(Path.GetFileName(attachmentPath))}[/]");

                        if (!AnsiConsole.Confirm("Submit?", defaultValue: true))
                        {
                            step = CreateStep.Type;
                            costs.Clear();
                            travelDetails = null;
                            attachmentPath = null;
                            break;
                        }

                        var create = new TravelExpenseCreate
                        {
                            Employee = new IdRef { Id = employeeId!.Value },
                            Project = projectId.HasValue ? new IdRef { Id = projectId.Value } : null,
                            Title = title,
                            TravelDetails = isTravelExpense ? travelDetails : null,
                            Costs = costs.Count > 0 ? costs : null,
                        };

                        var created = await client.Expense.CreateAsync(create);

                        if (attachmentPath is not null)
                        {
                            await client.ExpenseAttachment.UploadAsync(created.Id, attachmentPath);
                            AnsiConsole.MarkupLine($"[green]Uploaded attachment: {Markup.Escape(Path.GetFileName(attachmentPath))}[/]");
                        }

                        if (json)
                        {
                            OutputFormatter.Print(created, true);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]Created expense (ID: {created.Id}) — {Markup.Escape(created.Title ?? "")}[/]");
                        }
                        return;
                    }
                }
            }
        }, jsonOption);

        return cmd;
    }

    private static async Task<ExpenseCostCreate?> PromptCostAsync(TripletexClient client, bool isTravelExpense)
    {
        AnsiConsole.MarkupLine("[dim]Fetching cost categories...[/]");
        var categoriesResult = await client.Expense.SearchCostCategoriesAsync();
        var categories = (categoriesResult.Values ?? [])
            .Where(c => !c.IsInactive)
            .Where(c => isTravelExpense ? c.ShowOnTravelExpenses : c.ShowOnEmployeeExpenses)
            .OrderBy(c => c.Description)
            .ToList();

        if (categories.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No cost categories available.[/]");
            return null;
        }

        var selectedCategory = TimesheetCommand.FilterableSelect(
            "Select cost category",
            categories,
            c => c.Description ?? "",
            c => $"{c.Description ?? "Unnamed"} [dim]ID: {c.Id}[/]",
            filterSentinel: new ExpenseCostCategory { Id = -2 });

        if (selectedCategory is null) return null;

        AnsiConsole.MarkupLine("[dim]Fetching payment types...[/]");
        var paymentTypesResult = await client.Expense.SearchPaymentTypesAsync(
            showOnEmployeeExpenses: isTravelExpense ? null : true,
            showOnTravelExpenses: isTravelExpense ? true : null);
        var paymentTypes = (paymentTypesResult.Values ?? [])
            .Where(p => !p.IsInactive)
            .OrderBy(p => p.Description)
            .ToList();

        if (paymentTypes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No payment types available.[/]");
            return null;
        }

        var selectedPaymentType = TimesheetCommand.FilterableSelect(
            "Select payment type",
            paymentTypes,
            p => p.Description ?? "",
            p => $"{p.Description ?? "Unnamed"} [dim]ID: {p.Id}[/]",
            filterSentinel: new ExpensePaymentType { Id = -2 });

        if (selectedPaymentType is null) return null;

        var date = AnsiConsole.Prompt(
            new TextPrompt<string>("Date (yyyy-MM-dd):")
                .DefaultValue(DateTime.Today.ToString("yyyy-MM-dd")));

        var amount = AnsiConsole.Prompt(
            new TextPrompt<decimal>("Amount (incl. VAT):")
                .Validate(a => a > 0 ? ValidationResult.Success() : ValidationResult.Error("Must be > 0")));

        var comments = AnsiConsole.Prompt(
            new TextPrompt<string>("Comments:").AllowEmpty());

        var isPaidByEmployee = AnsiConsole.Confirm("Paid by employee?", defaultValue: true);

        return new ExpenseCostCreate
        {
            CostCategory = new IdRef { Id = selectedCategory.Id },
            PaymentType = new IdRef { Id = selectedPaymentType.Id },
            Date = date,
            AmountCurrencyIncVat = amount,
            Comments = string.IsNullOrWhiteSpace(comments) ? null : comments,
            IsPaidByEmployee = isPaidByEmployee,
        };
    }
}
