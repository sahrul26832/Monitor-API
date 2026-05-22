using System.Text.Json.Serialization;

namespace DemoResendInterface.Models.DTOs;

/// <summary>
/// DTO: Flat transport object returned to the client for API errors.
/// </summary>
public class ErrorDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("requestId")]
    public Guid RequestId { get; set; }

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("errorTimestamp")]
    public DateTime ErrorTimestamp { get; set; }

    [JsonPropertyName("errorCategory")]
    public string ErrorCategory { get; set; } = string.Empty;

    [JsonPropertyName("isResolved")]
    public bool IsResolved { get; set; }
}
