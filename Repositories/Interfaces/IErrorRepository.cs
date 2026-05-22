using DemoResendInterface.Models.Entities;

namespace DemoResendInterface.Repositories.Interfaces;

/// <summary>
/// Repository interface for ApiErrors table.
/// </summary>
public interface IErrorRepository
{
    Task<IEnumerable<ApiError>> FindAllAsync(string? category, string? errorCode, string? resolved);
    Task CreateAsync(ApiError entity);
    Task MarkResolvedAsync(Guid id);
}
