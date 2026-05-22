using System.Net;
using System.Text.Json;

namespace DemoResendInterface.Middleware;

/// <summary>
/// Centralized Exception Handler Middleware
/// - Logs full error details server-side
/// - Returns consistent error response format
/// - Never exposes internal details to client
/// - Classifies errors: validation, business, system, external
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Log full error details server-side
        _logger.LogError(ex, "[ERROR] {Method} {Path} | {ExceptionType}: {Message}",
            context.Request.Method, context.Request.Path, ex.GetType().Name, ex.Message);

        var (statusCode, errorType) = ClassifyException(ex);

        // Never expose internal details to client
        var clientMessage = statusCode >= 500
            ? "Internal server error"
            : ex.Message;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            error = new
            {
                type = errorType,
                message = clientMessage,
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Classify errors: validation, business, system, external
    /// </summary>
    private static (int StatusCode, string ErrorType) ClassifyException(Exception ex)
    {
        return ex switch
        {
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "business"),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "validation"),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "business"),
            HttpRequestException => ((int)HttpStatusCode.BadGateway, "external"),
            TaskCanceledException => ((int)HttpStatusCode.GatewayTimeout, "external"),
            _ => ((int)HttpStatusCode.InternalServerError, "system"),
        };
    }
}
