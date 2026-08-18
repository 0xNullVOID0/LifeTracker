namespace LifeTracker
{
    public static class GarminEndpoints
    {
        public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/garmin").WithTags("Garmin");

            // expects YYYY-MM-dd for date parameter
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

            return routes;
        }
    }
}
