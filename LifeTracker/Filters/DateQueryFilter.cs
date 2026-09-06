using LifeTracker.Infrastructure;

namespace LifeTracker.Filters;

// Public API contract: omitted ?date= means today in Time:ZoneID (Amsterdam).
// Python resolve_date() also defaults to today if the bridge is called with no query.
// .NET sync always sends an explicit yyyy-MM-dd so that path is only a fallback.
// DateQueryMiddleware already rejected garbage/empty strings so this class is only for specific date validation itself
public static class DateQueryFilter
{
    public static DateOnly Resolved(DateOnly? date) =>
        date ?? throw new InvalidOperationException("DateQueryFilter should have filled omitted ?date= with today.");

    public static async ValueTask<object?> ResolveDate(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        AppClock clock = ctx.HttpContext.RequestServices.GetRequiredService<AppClock>();
        DateOnly today = clock.Today();

        DateOnly date;
        // check if date param exists and sets target to either the date param or today
        if (ctx.Arguments.Count > 0 && ctx.Arguments[0] is DateOnly parsed && parsed != default)
            date = parsed;
        else
            date = today;

        if (date > today)
            return Results.BadRequest(new { error = "Cannot request non existent data from future dates." });

        // set route date param to today or the parsed date
        if (ctx.Arguments.Count > 0)
            ctx.Arguments[0] = date;

        return await next(ctx);
    }
}
