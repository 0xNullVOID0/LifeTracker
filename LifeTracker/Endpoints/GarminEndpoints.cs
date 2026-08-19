using LifeTracker.Services;

namespace LifeTracker.Endpoints
{
    public static class GarminEndpoints
    {
        public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/garmin").WithTags("Garmin");

            // TODO add proper openAPI documentation

            // optional date parameter expects YYYY-MM-dd format(defaults to today)
            group.MapGet("/heartrate", async (DateOnly? date, GarminBridgeService service) =>
            {
                var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);
                return await service.FetchHeartRateByDay(targetDate) is { } data ? Results.Ok(data) : Results.NotFound();
            }).WithName("FetchHeartRate");

            group.MapGet("/stress", async (DateOnly? date, GarminBridgeService service) =>
            {
                var targetDate = date ?? DateOnly.FromDateTime(DateTime.Now);
                return await service.FetchStressLevelByDay(targetDate) is { } data ? Results.Ok(data) : Results.NotFound();
            }).WithName("FetchStressLevel");

            group.MapGet("/health", async (GarminBridgeService service) =>
                await service.GarminBridgeHealthCheck() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("GarminBridgeHealthCheck").WithDescription("Checks if the Python GarminConnect bridge server is running");

            return routes;
        }
    }
}
