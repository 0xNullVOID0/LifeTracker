namespace LifeTracker.Middleware
{
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
            _logger.LogInformation("[API] {Method} {Path}", context.Request.Method,context.Request.Path);
            await _next(context);
        }
    }

}
