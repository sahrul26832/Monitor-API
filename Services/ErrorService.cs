using DemoResendInterface.Models.DTOs;
using DemoResendInterface.Models.Entities;
using DemoResendInterface.Repositories.Interfaces;
using DemoResendInterface.Services.Interfaces;

namespace DemoResendInterface.Services;

/// <summary>
/// Service: ErrorService — Core business logic.
/// No direct DB access, no HTTP concerns.
/// </summary>
public class ErrorService : IErrorService
{
    private readonly IErrorRepository _repo;

    public ErrorService(IErrorRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<ErrorDto>> GetAllAsync(
        string? category, string? errorCode, string? resolved)
    {
        var rows = await _repo.FindAllAsync(category, errorCode, resolved);
        return rows.Select(ToDto);
    }

    public async Task<object> CreateAsync(CreateErrorDto dto)
    {
        var id = Guid.NewGuid();
        var entity = new ApiError
        {
            Id = id,
            RequestId = dto.RequestId,
            ErrorCode = dto.ErrorCode,
            Message = dto.Message,
            StackTrace = dto.StackTrace,
            ErrorTimestamp = dto.ErrorTimestamp ?? DateTime.UtcNow,
            ErrorCategory = dto.ErrorCategory,
            IsResolved = dto.IsResolved ?? false,
        };

        await _repo.CreateAsync(entity);
        return new { id };
    }

    public async Task<object> ResolveAsync(Guid id)
    {
        await _repo.MarkResolvedAsync(id);
        return new { message = "Error marked as resolved" };
    }

    /// <summary>
    /// Map Entity → DTO.
    /// </summary>
    private static ErrorDto ToDto(ApiError row)
    {
        return new ErrorDto
        {
            Id = row.Id,
            RequestId = row.RequestId,
            ErrorCode = row.ErrorCode,
            Message = row.Message,
            StackTrace = row.StackTrace,
            ErrorTimestamp = row.ErrorTimestamp,
            ErrorCategory = row.ErrorCategory,
            IsResolved = row.IsResolved,
        };
    }
}
