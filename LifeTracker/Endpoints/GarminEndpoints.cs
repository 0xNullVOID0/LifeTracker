using LifeTracker.Services;

namespace LifeTracker.Endpoints
{
    public static class GarminEndpoints
    {
        public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/garmin").WithTags("Garmin");

            // TODO add proper openAPI documentation
            // TODO cancellation tokens?

            static bool IsFuture(DateOnly date) =>
                date > DateOnly.FromDateTime(DateTime.Today);

            static IResult? ValidateDate(DateOnly? date, out DateOnly targetDate)
            {
                targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
                if (IsFuture(targetDate))
                    return Results.BadRequest(new { error = "Cannot request non existent data from future dates." });
                return null; // means OK(no errors found)
            }

            static IResult OkOrNoContent<T>(T? data) =>
                data is not null ? Results.Ok(data) : Results.NoContent();

            // optional date parameter expects YYYY-MM-dd format(defaults to today)
            group.MapGet("/heartrate", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.FetchHeartRateByDay(targetDate);
                return OkOrNoContent(data);
            }).WithName("FetchHeartRate");

            group.MapGet("/stress", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.FetchStressLevelByDay(targetDate);
                return OkOrNoContent(data);
            }).WithName("FetchStressLevel");

            group.MapGet("/sleep", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.FetchSleepByDay(targetDate);
                return OkOrNoContent(data);
            })
            .WithName("FetchSleep");
            group.MapGet("/sync/all", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.SyncAllDataByDay(targetDate);
                return OkOrNoContent(data);
            }).WithName("SyncAllData");

            group.MapGet("/health", async (GarminBridgeService service) =>
                await service.GarminBridgeHealthCheck() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("GarminBridgeHealthCheck").WithDescription("Checks if the Python GarminConnect bridge server is running");

            return routes;
        }
    }
}
