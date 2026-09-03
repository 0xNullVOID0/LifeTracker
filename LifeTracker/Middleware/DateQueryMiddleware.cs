using System.Globalization;

namespace LifeTracker.Middleware;

public sealed class DateQueryMiddleware
{
    private readonly RequestDelegate _next;

    public DateQueryMiddleware(RequestDelegate next) => _next = next;

    // Prevents empty or faulty date(time) params from causing BadHttpRequestException due to failing to bind empty or improper param data to proper DateOnly object before ever even hitting the endpoint function code
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip middleware if no params or if request isn't on an actual api endpoint
        if (context.Request.Query.Count == 0 ||
            path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.Query.TryGetValue("date", out var raw))
        {
            var text = raw.ToString();
            if (string.IsNullOrWhiteSpace(text) || !DateOnly.TryParseExact(text, "yyyy-MM-dd", out _))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid date/format. Use YYYY-MM-DD like 2026-08-24" });
                return;
            }
        }

        // Currently for the range query of ActivityWatch but others will need it soon as well
        foreach (var paramName in new[] { "start", "end" })
        {
            if (context.Request.Query.TryGetValue(paramName, out var rawTimestamp))
            {
                var text = rawTimestamp.ToString();
                if (string.IsNullOrWhiteSpace(text) || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = $"Invalid '{paramName}' format. Expected ISO-8601 (e.g., 2026-09-03T13:00:00Z)." });
                    return;
                }
            }
        }

        await _next(context);
    }
}
