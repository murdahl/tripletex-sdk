using System.Text.Json.Serialization;

namespace Tripletex.Cli.Configuration;

public sealed class CliConfig
{
    [JsonPropertyName("consumerToken")]
    public string? ConsumerToken { get; set; }

    [JsonPropertyName("employeeToken")]
    public string? EmployeeToken { get; set; }

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    [JsonPropertyName("defaultProjectId")]
    public int? DefaultProjectId { get; set; }

    [JsonPropertyName("defaultProjectName")]
    public string? DefaultProjectName { get; set; }

    [JsonPropertyName("defaultActivityId")]
    public int? DefaultActivityId { get; set; }

    [JsonPropertyName("defaultActivityName")]
    public string? DefaultActivityName { get; set; }

    [JsonPropertyName("defaultEmployeeId")]
    public int? DefaultEmployeeId { get; set; }

    [JsonPropertyName("defaultEmployeeName")]
    public string? DefaultEmployeeName { get; set; }
}
