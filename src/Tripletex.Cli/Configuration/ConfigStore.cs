using System.Text.Json;

namespace Tripletex.Cli.Configuration;

public static class ConfigStore
{
    private static readonly string ConfigDir = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        ".tripletex");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static CliConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new CliConfig();

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<CliConfig>(json, JsonOptions) ?? new CliConfig();
    }

    public static void Save(CliConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public static string GetConsumerToken(CliConfig config) =>
        System.Environment.GetEnvironmentVariable("TRIPLETEX_CONSUMER_TOKEN")
        ?? config.ConsumerToken
        ?? throw new InvalidOperationException(
            "Consumer token not set. Use 'config set --consumer-token <token>' or set TRIPLETEX_CONSUMER_TOKEN.");

    public static string GetEmployeeToken(CliConfig config) =>
        System.Environment.GetEnvironmentVariable("TRIPLETEX_EMPLOYEE_TOKEN")
        ?? config.EmployeeToken
        ?? throw new InvalidOperationException(
            "Employee token not set. Use 'config set --employee-token <token>' or set TRIPLETEX_EMPLOYEE_TOKEN.");
}
