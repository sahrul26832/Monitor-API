using DemoResendInterface.Models.Entities;

namespace DemoResendInterface.Repositories.Interfaces;

/// <summary>
/// Repository interface for ApiRequests table.
/// </summary>
public interface IRequestRepository
{
    Task<IEnumerable<ApiRequest>> FindAllAsync(string? method, string? status, string? url, string? dateFrom, string? dateTo);
    Task<ApiRequest?> FindByIdAsync(Guid id);
    Task CreateAsync(ApiRequest entity);
    Task UpdateStatusAsync(Guid id, string status);
}
