using System.Text.Json.Serialization;

namespace DemoResendInterface.Models.DTOs;

/// <summary>
/// DTO: Input model for creating a new error log.
/// </summary>
public class CreateErrorDto
{
    [JsonPropertyName("requestId")]
    public Guid RequestId { get; set; }

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("errorTimestamp")]
    public DateTime? ErrorTimestamp { get; set; }

    [JsonPropertyName("errorCategory")]
    public string ErrorCategory { get; set; } = string.Empty;

    [JsonPropertyName("isResolved")]
    public bool? IsResolved { get; set; }
}
