using System.Diagnostics;

namespace LifeTracker.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Add logging to every request
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var request = context.Request;
        var query = request.QueryString.HasValue ? request.QueryString.Value : "";

        // Set log level based on status code
        var status = context.Response.StatusCode;
        var level = status >= 500 ? LogLevel.Error
                  : status >= 400 ? LogLevel.Warning
                  : LogLevel.Information;

        _logger.LogInformation(
            "[API] {Method} {Path}{Query} - {StatusCode} in {ElapsedMs}ms",
            request.Method,
            request.Path.Value,
            query,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
