namespace LifeTracker.Middleware;

public sealed class DateQueryMiddleware
{
    private readonly RequestDelegate _next;

    public DateQueryMiddleware(RequestDelegate next) => _next = next;

    // Prevents empty or faulty date params from causing BadHttpRequestException due to failing to bind empty or improper param data to proper DateOnly object before ever even hitting the endpoint function code
    public async Task InvokeAsync(HttpContext context)
    {
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

        await _next(context);
    }
}
