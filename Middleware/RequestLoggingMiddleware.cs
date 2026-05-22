using System.Diagnostics;

namespace DemoResendInterface.Middleware;

/// <summary>
/// Request Logging Middleware
/// Logs API entry/exit with correlation ID, timestamp, duration.
/// Follows architecture.md cross-cutting logging requirements.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or extract correlation ID
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? GenerateCorrelationId();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // Log entry
        _logger.LogInformation("[REQ] {CorrelationId} | {Method} {Path} | IP: {IP}",
            correlationId, context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);

        await _next(context);

        stopwatch.Stop();

        // Log exit
        _logger.LogInformation("[RES] {CorrelationId} | {Method} {Path} | {StatusCode} | {Duration}ms",
            correlationId, context.Request.Method, context.Request.Path,
            context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }

    private static string GenerateCorrelationId()
    {
        return $"cid-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
