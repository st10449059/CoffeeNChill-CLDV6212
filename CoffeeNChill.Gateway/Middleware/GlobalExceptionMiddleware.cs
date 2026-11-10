using System.Text.Json;

namespace CoffeeNChill.Gateway.Middleware;

/// <summary>
/// Handles unexpected API exceptions consistently.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Creates the global error handler.
    /// </summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes requests and handles unhandled exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled Gateway error. Trace ID: {TraceId}",
                context.TraceIdentifier);

            if (context.Response.HasStarted ||
                context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            context.Response.ContentType =
                "application/problem+json";

            var response = new
            {
                type = "about:blank",
                title = "Internal Server Error",
                status = 500,
                detail = "An unexpected server error occurred.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response),
                context.RequestAborted);
        }
    }
}
