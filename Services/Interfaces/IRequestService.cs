using DemoResendInterface.Models.DTOs;

namespace DemoResendInterface.Services.Interfaces;

/// <summary>
/// Service interface for request business logic.
/// </summary>
public interface IRequestService
{
    Task<IEnumerable<RequestDto>> GetAllAsync(string? method, string? status, string? url, string? dateFrom, string? dateTo);
    Task<RequestDto?> GetByIdAsync(Guid id);
    Task<object> CreateAsync(CreateRequestDto dto);
    Task<object> ResendAsync(Guid id);
    Task<object> IgnoreAsync(Guid id);
}
