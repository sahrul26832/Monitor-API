namespace DemoResendInterface.Models.Entities;

/// <summary>
/// Entity: Maps 1:1 with the ApiRequests SQL Server table.
/// Used only in Repository layer — never expose directly to client.
/// </summary>
public class ApiRequest
{
    public Guid Id { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string? AppName { get; set; }
    public string Url { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? Headers { get; set; }
    public string? Body { get; set; }
    public string? ClientIpAddress { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public string Status { get; set; } = "PENDING";
    public int? ResponseStatusCode { get; set; }
    public int ResponseTime { get; set; }
    public DateTime CreatedAt { get; set; }
}
