using System.Text.Json;
using DemoResendInterface.Models.DTOs;
using DemoResendInterface.Models.Entities;
using DemoResendInterface.Repositories.Interfaces;
using DemoResendInterface.Services.Interfaces;

namespace DemoResendInterface.Services;

/// <summary>
/// Service: RequestService — Core business logic.
/// No direct DB access, no HTTP concerns.
/// </summary>
public class RequestService : IRequestService
{
    private readonly IRequestRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RequestService> _logger;

    public RequestService(
        IRequestRepository repo,
        IHttpClientFactory httpClientFactory,
        ILogger<RequestService> logger)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<RequestDto>> GetAllAsync(
        string? method, string? status, string? url, string? dateFrom, string? dateTo)
    {
        var rows = await _repo.FindAllAsync(method, status, url, dateFrom, dateTo);
        return rows.Select(ToDto);
    }

    public async Task<RequestDto?> GetByIdAsync(Guid id)
    {
        var row = await _repo.FindByIdAsync(id);
        return row is null ? null : ToDto(row);
    }

    public async Task<object> CreateAsync(CreateRequestDto dto)
    {
        var id = Guid.NewGuid();
        var entity = new ApiRequest
        {
            Id = id,
            ApplicationName = dto.ApplicationName,
            AppName = dto.AppName,
            Url = dto.Url,
            HttpMethod = dto.HttpMethod,
            Headers = dto.Headers is not null ? JsonSerializer.Serialize(dto.Headers) : null,
            Body = dto.Body,
            ClientIpAddress = dto.ClientIpAddress,
            RequestTimestamp = dto.RequestTimestamp ?? DateTime.UtcNow,
            Status = dto.Status ?? "PENDING",
            ResponseStatusCode = dto.ResponseStatusCode,
            ResponseTime = dto.ResponseTime ?? 0,
        };

        await _repo.CreateAsync(entity);
        return new { id };
    }

    /// <summary>
    /// Resend a failed request — re-calls the original URL and logs the result.
    /// </summary>
    public async Task<object> ResendAsync(Guid id)
    {
        var original = await _repo.FindByIdAsync(id);
        if (original is null)
            throw new KeyNotFoundException("Request not found");

        var startTime = DateTime.UtcNow;
        var resendStatus = "SUCCESS";
        int? resendStatusCode = null;
        string? errorMessage = null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(new System.Net.Http.HttpMethod(original.HttpMethod), original.Url);

            // Parse and apply original headers
            if (!string.IsNullOrEmpty(original.Headers))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(original.Headers);
                    if (headers is not null)
                    {
                        foreach (var kvp in headers)
                        {
                            // Skip content-type header — it's set on the content itself
                            if (kvp.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                continue;
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Failed to parse headers for request {Id}", id);
                }
            }

            // Attach body for POST/PUT/PATCH
            if (!string.IsNullOrEmpty(original.Body) &&
                new[] { "POST", "PUT", "PATCH" }.Contains(original.HttpMethod, StringComparer.OrdinalIgnoreCase))
            {
                request.Content = new StringContent(original.Body, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await client.SendAsync(request);
            resendStatusCode = (int)response.StatusCode;
            resendStatus = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR";
        }
        catch (Exception ex)
        {
            resendStatus = "ERROR";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Resend failed for request {Id}", id);
        }

        var responseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

        // Log the resend as a new request entry
        var newId = Guid.NewGuid();
        await _repo.CreateAsync(new ApiRequest
        {
            Id = newId,
            ApplicationName = original.ApplicationName,
            AppName = original.AppName,
            Url = original.Url,
            HttpMethod = original.HttpMethod,
            Headers = original.Headers,
            Body = original.Body,
            ClientIpAddress = original.ClientIpAddress,
            RequestTimestamp = DateTime.UtcNow,
            Status = resendStatus,
            ResponseStatusCode = resendStatusCode,
            ResponseTime = responseTime,
        });

        var success = resendStatus == "SUCCESS";
        return new
        {
            success,
            message = success
                ? "Resend สำเร็จ: Request ถูกส่งซ้ำเรียบร้อยแล้ว"
                : $"Resend ไม่สำเร็จ: {errorMessage ?? "Backend service returned error"}",
            newRequestId = newId,
        };
    }

    public async Task<object> IgnoreAsync(Guid id)
    {
        await _repo.UpdateStatusAsync(id, "IGNORED");
        return new { message = "Request ถูก Ignore เรียบร้อยแล้ว" };
    }

    /// <summary>
    /// Map Entity → DTO (never expose raw DB entity to client).
    /// </summary>
    private static RequestDto ToDto(ApiRequest row)
    {
        object? parsedHeaders = null;
        if (!string.IsNullOrEmpty(row.Headers))
        {
            try { parsedHeaders = JsonSerializer.Deserialize<object>(row.Headers); }
            catch { parsedHeaders = row.Headers; }
        }

        return new RequestDto
        {
            Id = row.Id,
            ApplicationName = row.ApplicationName,
            AppName = row.AppName,
            Url = row.Url,
            HttpMethod = row.HttpMethod,
            Headers = parsedHeaders,
            Body = row.Body,
            ClientIpAddress = row.ClientIpAddress,
            RequestTimestamp = row.RequestTimestamp,
            Status = row.Status,
            ResponseStatusCode = row.ResponseStatusCode,
            ResponseTime = row.ResponseTime,
        };
    }
}
