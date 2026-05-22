using DemoResendInterface.Models.DTOs;

namespace DemoResendInterface.Services.Interfaces;

/// <summary>
/// Service interface for error business logic.
/// </summary>
public interface IErrorService
{
    Task<IEnumerable<ErrorDto>> GetAllAsync(string? category, string? errorCode, string? resolved);
    Task<object> CreateAsync(CreateErrorDto dto);
    Task<object> ResolveAsync(Guid id);
}
