namespace DemoResendInterface.Models.Entities;

/// <summary>
/// Entity: Maps 1:1 with the ApiErrors SQL Server table.
/// Used only in Repository layer — never expose directly to client.
/// </summary>
public class ApiError
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
    public DateTime ErrorTimestamp { get; set; }
    public string ErrorCategory { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
}
