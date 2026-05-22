using System.Text.Json.Serialization;

namespace DemoResendInterface.Models.DTOs;

/// <summary>
/// DTO: Flat transport object returned to the client for API requests.
/// </summary>
public class RequestDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; set; } = string.Empty;

    [JsonPropertyName("appName")]
    public string? AppName { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("httpMethod")]
    public string HttpMethod { get; set; } = string.Empty;

    [JsonPropertyName("headers")]
    public object? Headers { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("clientIpAddress")]
    public string? ClientIpAddress { get; set; }

    [JsonPropertyName("requestTimestamp")]
    public DateTime RequestTimestamp { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("responseStatusCode")]
    public int? ResponseStatusCode { get; set; }

    [JsonPropertyName("responseTime")]
    public int ResponseTime { get; set; }
}
