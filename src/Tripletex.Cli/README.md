# Tripletex CLI

A command-line tool for interacting with the Tripletex API — log hours, manage projects, and more.

## Setup

```bash
# Build
dotnet build src/Tripletex.Cli

# Configure your API tokens
dotnet run --project src/Tripletex.Cli -- config set \
  --consumer-token <your-consumer-token> \
  --employee-token <your-employee-token>

# Use test environment (optional, defaults to production)
dotnet run --project src/Tripletex.Cli -- config set --environment test
```

Tokens can also be set via environment variables:

```bash
export TRIPLETEX_CONSUMER_TOKEN=<token>
export TRIPLETEX_EMPLOYEE_TOKEN=<token>
```

Environment variables take precedence over the config file.

## Quick start — logging hours

The fastest way to get started:

```bash
# Just run log with no arguments — it walks you through everything:
tripletex timesheet log
```

This will interactively prompt you for:

1. **Project** — if no default is set, fetches your projects and lets you pick one
2. **Activity** — if no default is set, fetches recent activities and lets you pick one
3. **Hours** — how many hours to log
4. **Date** — defaults to today, press Enter to accept
5. **Comment** — optional, press Enter to skip

After selecting a project/activity you'll be asked whether to save it as the default for next time.

Once defaults are saved, you can also pass arguments directly:

```bash
# Quick log with defaults
tripletex timesheet log 7.5
tripletex timesheet log 7.5 --date 2026-03-04 --comment "Feature work"

# Log a full week (Mon-Fri)
tripletex timesheet log-week 2026-03-02 7.5 7.5 7.5 7.5 7.5
```

## Managing defaults

```bash
# Interactively pick a default project
tripletex project select

# Interactively pick a default activity
tripletex activity select

# Clear defaults (next log will prompt again)
tripletex project reset
tripletex activity reset
```

## Commands

### config

```bash
tripletex config set --consumer-token <t> --employee-token <t> [--environment test|production]
tripletex config show
```

### timesheet

```bash
# Interactive — prompts for everything not provided
tripletex timesheet log
tripletex timesheet log <hours>
tripletex timesheet log <hours> [--date yyyy-MM-dd] [--comment "..."] [--project-id N] [--activity-id N]

# Batch — log a full week
tripletex timesheet log-week <week-start> <mon> <tue> <wed> <thu> <fri> [--sat N] [--sun N] [--comment "..."]

# Read
tripletex timesheet get <id>
tripletex timesheet list [--from-date yyyy-MM-dd] [--to-date yyyy-MM-dd] [--employee-id N] [--project-id N]
tripletex timesheet recent
tripletex timesheet total-hours <employee-id> <from-date> <to-date>

# Manage
tripletex timesheet delete <id>
tripletex timesheet approve-week <employee-id> <week-start>
```

### project

```bash
tripletex project list
tripletex project get <id>
tripletex project search --name "Portal"
tripletex project select          # interactive picker, saves as default
tripletex project reset           # clear saved default
```

### activity

```bash
tripletex activity select [--project-id N]   # interactive picker, saves as default
tripletex activity reset                     # clear saved default
```

### employee / customer / supplier

```bash
tripletex employee list
tripletex employee get <id>
tripletex employee search --first-name "Ole" --last-name "Urdahl"

tripletex customer list
tripletex customer get <id>
tripletex customer search --name "Acme"

tripletex supplier list
tripletex supplier get <id>
tripletex supplier search --name "Widgets"
```

### invoice

```bash
tripletex invoice list [--from-date yyyy-MM-dd] [--to-date yyyy-MM-dd] [--customer-id N]
tripletex invoice get <id>
```

## Global options

| Option   | Description              |
|----------|--------------------------|
| `--json` | Output results as JSON   |
| `--help` | Show help for any command |

```bash
# Get JSON output for scripting
tripletex timesheet recent --json
tripletex project list --json | jq '.[].name'
```

## Configuration file

Stored at `~/.tripletex/config.json`. Example:

```json
{
  "consumerToken": "your-token",
  "employeeToken": "your-token",
  "environment": "production",
  "defaultProjectId": 42,
  "defaultProjectName": "Internal Tools",
  "defaultActivityId": 15,
  "defaultActivityName": "Development"
}
```

## Running without install

```bash
dotnet run --project src/Tripletex.Cli -- <command>
```

To install as a global tool alias, publish and add to your PATH:

```bash
dotnet publish src/Tripletex.Cli -c Release -o ~/.local/bin
```
