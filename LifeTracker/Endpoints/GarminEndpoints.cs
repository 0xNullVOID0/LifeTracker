using LifeTracker.Entities.Garmin;
using LifeTracker.Filters;
using LifeTracker.Services;
using static LifeTracker.Endpoints.EndpointHelpers;

namespace LifeTracker.Endpoints;

public static class GarminEndpoints
{
    public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
    {
        // Set route prefix and Error filter for group to turn expected Exceptions into proper ProblemDetails without unneccesary dev exception page bloat
        var group = routes.MapGroup("/garmin").WithTags("Garmin").AddEndpointFilter(GarminBridgeErrorFilter.Handle);

        // TODO cancellation tokens?

        // Optional date parameter for majority of routes that expects YYYY-MM-dd format(defaults to today)

        // TODO add limits, rate limits, .WithLimit
        // Get Endpoints
        group.MapGet("/all", async (GarminBridgeService service) => // TODO add optional start and or end range to it
            {
                var data = await service.GetAllGarminDays();
                return data.Count > 0 ? Results.Ok(data) : Results.NoContent();
            })
            .WithName("GetAllGarminDays").WithSummary("Get all stored Garmin days data from the database")
            .WithDescription("Returns list of composite GarminDay objects for every day that has heart, stress and or sleep from the database")
            .Produces<IReadOnlyList<GarminDay>>(StatusCodes.Status200OK).Produces(StatusCodes.Status204NoContent);

        group.MapPost("/sync/backfill", async (int? days, GarminBridgeService service) =>
                await GarminEndpointExtensions.EnsureBridgeAvailable(service) ?? Results.Ok(await service.SyncRecentDays(days ?? 14)))
            .WithName("SyncRecentDays").WithSummary("Sync recent Garmin days into the the database")
            .WithDescription("Starts backfilling, syncing all Garmin data from the oldest given date to today.")
            .Produces<GarminBridgeService.BackfillResult>(200).ProducesProblem(StatusCodes.Status400BadRequest);

        
        // Add DateQueryFilter to all routes below, handles date validation and sets date to today by default if no param given
        RouteGroupBuilder dated = group.MapGroup(string.Empty).AddEndpointFilter(DateQueryFilter.ResolveDate);

        dated.MapGarminGet("/day", (service, date) => service.GetAllDataByDay(date))
            .ConfigureRoute<GarminDay>("GetAllDataByDay", "Get all Garmin data for a specific day",
                "Returns all user's available Garmin data for a specific day from the database");

        dated.MapGarminGet("/heartrate", (service, date) => service.GetHeartRateByDay(date))
            .ConfigureRoute<DailyHeartRate>("GetHeartRateByDay", "Get stored heart rate for a specific day",
                "Returns DailyHeartRate and its HeartRateSamples from the database");

        dated.MapGarminGet("/stress", (service, date) => service.GetStressByDay(date))
            .ConfigureRoute<DailyStress>("GetStressByDay", "Get stored stress for a specific day",
                "Returns DailyStress from the database");

        dated.MapGarminGet("/sleep", (service, date) => service.GetSleepByDay(date))
            .ConfigureRoute<DailySleep>("GetSleepByDay", "Get stored sleep for a specific day",
                "Returns DailySleep from the database");
        
        dated.MapGarminGet("/sleep/awake-window", (service, date) => service.CalcAwakeWindow(date))
            .ConfigureRoute<DailySleep>("CalcAwakeWindow", "Calculates the awake window for the given day",
                "Calculates and returns the AwakeWindow by measuring the gap between the end of the preceding sleep session and the start of the subsequent sleep session for the given day.");
        
        dated.MapGarminPost("/sync/heartrate", (service, date) => service.SyncHeartRateByDay(date))
            .ConfigureRoute<DailyHeartRate>("SyncHeartRateByDay", "Sync Garmin heart rate data",
                "Fetches and syncs user's heart rate data for a specific day to the database");

        dated.MapGarminPost("/sync/stress", (service, date) => service.SyncStressLevelByDay(date))
            .ConfigureRoute<DailyStress>("SyncStressLevelByDay", "Sync Garmin stress data",
                "Fetches and syncs user's stress level data for a specific day to the database");

        dated.MapGarminPost("/sync/sleep", (service, date) => service.SyncSleepByDay(date))
            .ConfigureRoute<DailySleep>("SyncSleepByDay", "Sync Garmin sleep data",
                "Fetches and syncs user's sleep data for a specific day to the database");

        dated.MapGarminPost("/sync/day", (service, date) => service.SyncAllDataByDay(date))
            .ConfigureRoute<GarminDay>("SyncAllDataByDay", "Sync all Garmin data for a specific day",
                "Fetches and syncs all user's available Garmin data for a specific day to the database");

        return routes;
    }
}

public static class GarminEndpointExtensions
{
    public static async Task<IResult?> EnsureBridgeAvailable(GarminBridgeService service) =>
        await service.IsBridgeAvailable() ? null : Results.Problem(title: "Service Unavailable", 
            detail: "The Garmin Connect Bridge is not running or unreachable.", 
            statusCode: StatusCodes.Status503ServiceUnavailable);
    
    public static RouteHandlerBuilder MapGarminGet<T>(this IEndpointRouteBuilder routes, string route, Func<GarminBridgeService, DateOnly, Task<T?>> action) where T : class => 
        routes.MapGet(route, async (DateOnly? date, GarminBridgeService service) => OkOrNoContent(await action(service, DateQueryFilter.Resolved(date))));

    public static RouteHandlerBuilder MapGarminPost<T>(this IEndpointRouteBuilder routes, string route, Func<GarminBridgeService, DateOnly, Task<T?>> action) where T : class => 
        routes.MapPost(route, async (DateOnly? date, GarminBridgeService service) => await EnsureBridgeAvailable(service) ?? OkOrNoContent(await action(service, DateQueryFilter.Resolved(date))));
}
