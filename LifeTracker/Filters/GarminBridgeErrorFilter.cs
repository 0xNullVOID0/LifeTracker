namespace LifeTracker.Filters;

public static class GarminBridgeErrorFilter
{
    public static async ValueTask<object?> Handle(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try
        {
            return await next(ctx);
        }
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "Garmin Connect Bridge Error",
                instance: $"{ctx.HttpContext.Request.Path}{ctx.HttpContext.Request.QueryString}",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
