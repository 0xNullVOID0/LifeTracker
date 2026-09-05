using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using static LifeTracker.Endpoints.EndpointHelpers;

namespace LifeTracker.Endpoints;

public static class GarminEndpoints
{
    public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/garmin").WithTags("Garmin");

        // TODO cancellation tokens?

        // Optional date parameter for majority of routes that expects YYYY-MM-dd format(defaults to today)

        // TODO add limits, rate limits, .WithLimit
        // Get Endpoints
        group.MapGet("/all", async (GarminBridgeService service) => // TODO add optional start and or end range to it
            {
                var data = await service.GetAllGarminDays();
                return data.Count > 0 ? Results.Ok(data) : Results.NoContent();
            })
            .WithName("GetAllGarminDays").WithSummary("Get all stored Garmin days data from DB")
            .WithDescription(
                "Returns list of composite GarminDay objects for every day that has heart, stress and or sleep from DB")
            .Produces<IReadOnlyList<GarminDay>>(StatusCodes.Status200OK).Produces(StatusCodes.Status204NoContent);

        group.MapGet("/day", async (DateOnly? date, GarminBridgeService service) =>
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.GetAllDataByDay(target)))
            .ConfigureRoute<GarminDay>("GetAllDataByDay", "Get all Garmin data for a specific day",
                "Returns all user's available Garmin data for a specific day from DB");

        group.MapGet("/heartrate", async (DateOnly? date, GarminBridgeService service) =>
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.GetHeartRateByDay(target)))
            .ConfigureRoute<DailyHeartRate>("GetHeartRateByDay", "Get stored heart rate for a specific day",
                "Returns DailyHeartRate and its HeartRateSamples from DB");

        group.MapGet("/stress", async (DateOnly? date, GarminBridgeService service) =>
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.GetStressByDay(target)))
            .ConfigureRoute<DailyStress>("GetStressByDay", "Get stored stress for a specific day",
                "Returns DailyStress from DB");

        group.MapGet("/sleep", async (DateOnly? date, GarminBridgeService service) =>
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.GetSleepByDay(target)))
            .ConfigureRoute<DailySleep>("GetSleepByDay", "Get stored sleep for a specific day",
                "Returns DailySleep from DB");

        group.MapGet("/health", async (GarminBridgeService service) =>
                await service.GarminBridgeHealthCheck() is { } data ? Results.Ok(data) : Results.StatusCode(503))
            .WithName("GarminBridgeHealthCheck")
            .WithDescription("Checks if the Python Garmin Connect bridge server is running").ExcludeFromDescription();


        // Sync Endpoints
        group.MapPost("/sync/heartrate", async (DateOnly? date, GarminBridgeService service) =>
                await EnsureBridgeAvailable(service) ??
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.SyncHeartRateByDay(target)))
            .ConfigureRoute<DailyHeartRate>("SyncHeartRateByDay", "Sync Garmin heart rate data",
                "Fetches and syncs user's heart rate data for a specific day");

        group.MapPost("/sync/stress", async (DateOnly? date, GarminBridgeService service) =>
                await EnsureBridgeAvailable(service) ??
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.SyncStressLevelByDay(target)))
            .ConfigureRoute<DailyStress>("SyncStressLevelByDay", "Sync Garmin stress data",
                "Fetches and syncs user's stress level data for a specific day");

        group.MapPost("/sync/sleep", async (DateOnly? date, GarminBridgeService service) =>
                await EnsureBridgeAvailable(service) ??
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.SyncSleepByDay(target)))
            .ConfigureRoute<DailySleep>("SyncSleepByDay", "Sync Garmin sleep data",
                "Fetches and syncs user's sleep data for a specific day");

        group.MapPost("/sync/day", async (DateOnly? date, GarminBridgeService service) =>
                await EnsureBridgeAvailable(service) ??
                ValidateDate(date, out var target) ?? OkOrNoContent(await service.SyncAllDataByDay(target)))
            .ConfigureRoute<GarminDay>("SyncAllDataByDay", "Sync all Garmin data for a specific day",
                "Fetches and syncs all user's available Garmin data for a specific day");

        group.MapPost("/sync/backfill", async (int? days, GarminBridgeService service) =>
                await EnsureBridgeAvailable(service) ??
                Results.Ok(await service.SyncRecentDays(days ?? 14)))
            .WithName("SyncRecentDays").WithSummary("Sync recent Garmin days into the database")
            .WithDescription("Starts backfilling, syncing all Garmin data from the oldest given date to today.")
            .Produces<GarminBridgeService.BackfillResult>(200).ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }

    // Helpers
    public static async Task<IResult?> EnsureBridgeAvailable(GarminBridgeService service) =>
        await service.IsBridgeAvailable()
            ? null
            : Results.Problem(title: "Service Unavailable",
                detail: "The Garmin Connect Bridge is not running or unreachable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
}
