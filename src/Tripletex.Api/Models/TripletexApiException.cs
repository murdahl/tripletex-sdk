using System.Text.Json.Serialization;

namespace Tripletex.Api.Models;

public sealed class TripletexApiException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }
    public string? DeveloperMessage { get; }
    public string? RequestId { get; }
    public IReadOnlyList<ValidationMessage> ValidationMessages { get; }

    public TripletexApiException(
        int statusCode,
        string? message,
        string? errorCode = null,
        string? developerMessage = null,
        string? requestId = null,
        IReadOnlyList<ValidationMessage>? validationMessages = null)
        : base(message ?? $"Tripletex API error {statusCode}")
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        DeveloperMessage = developerMessage;
        RequestId = requestId;
        ValidationMessages = validationMessages ?? [];
    }
}

public sealed class TripletexErrorResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("developerMessage")]
    public string? DeveloperMessage { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("validationMessages")]
    public List<ValidationMessage>? ValidationMessages { get; set; }
}

public sealed class ValidationMessage
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
