using LifeTracker.Entities;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LifeTracker.Endpoints;

    public static class GarminEndpoints
    {
        public static IEndpointRouteBuilder MapGarminEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/garmin").WithTags("Garmin");

            // TODO add proper openAPI documentation
            // TODO cancellation tokens?


        // TODO move stuff to endpoint filters? for global use
            static bool IsFuture(DateOnly date) =>
                date > DateOnly.FromDateTime(DateTime.Today);

            static IResult? ValidateDate(DateOnly? date, out DateOnly targetDate)
            {
                targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
                if (IsFuture(targetDate))
                    return Results.BadRequest(new { error = "Cannot request non existent data from future dates." });
                return null; // means OK(no errors found)
            }

        static Results<Ok<T>, NoContent> OkOrNoContent<T>(T? data) =>
            data is not null ? TypedResults.Ok(data) : TypedResults.NoContent();

            // optional date parameter expects YYYY-MM-dd format(defaults to today)
        group.MapGet("/sync/heartrate", async (DateOnly? date, GarminBridgeService service) => {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.SyncHeartRateByDay(targetDate);
            return OkOrNoContent<DailyHeartRate>(data);
        }).WithName("SyncHeartRateByDay").WithSummary("Sync Garmin Heart Rate data")
          .WithDescription("Fetches and syncs user's heart rate data for a specific day from the Python Garmin Connect Bridge API to DB")
          .Produces<DailyHeartRate>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);


        group.MapGet("/sync/stress", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.SyncStressLevelByDay(targetDate);
            return OkOrNoContent<DailyStress>(data);
        }).WithName("SyncStressLevelByDay").WithSummary("Sync Garmin Stress Level data")
          .WithDescription("Fetches and syncs user's stress level data for a specific day from the Python Garmin Connect Bridge API to DB")
          .Produces<DailyStress>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/sync/sleep", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.SyncSleepByDay(targetDate);
            return OkOrNoContent<DailySleep>(data);
        }).WithName("SyncSleep").WithSummary("Sync Garmin Sleep data")
          .WithDescription("Fetches and syncs user's sleep data for a specific day from the Python Garmin Connect Bridge API to DB")
          .Produces<DailySleep>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);
                return OkOrNoContent(data);
        }).WithName("SyncSleep").WithSummary("Fetch Garmin Sleep data")
          .WithDescription("Fetches and syncs user's sleep data for a specific day from the Python Garmin Connect Bridge API to DB");

            group.MapGet("/day", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.GetAllDataByDay(targetDate);
                return OkOrNoContent(data);
        }).WithName("GetAllDataByDay").WithSummary("Sync all Garmin data for a specific day")
          .WithDescription("Fetches and syncs all user's available Garmin data for a specific day from the Python Garmin Connect Bridge API")
          .Produces<GarminDay>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);


            group.MapGet("/sync/all", async (DateOnly? date, GarminBridgeService service) =>
            {
                if (ValidateDate(date, out var targetDate) is { } error)
                    return error;

                var data = await service.SyncAllDataByDay(targetDate);
                return OkOrNoContent(data);
        }).WithName("SyncAllDataByDay").WithSummary("Sync all Garmin data for a specific day")
          .WithDescription("Fetches and syncs all user's available Garmin data for a specific day from the Python Garmin Connect Bridge API")
          .Produces<GarminDay>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);


        group.MapPost("/sync/backfill", async (int? days, GarminBridgeService service) =>
        {
            var data = await service.SyncRecentDays(days ?? 14);
            return Results.Ok(data);
        }).WithName("SyncRecentDays").WithSummary("Sync recent Garmin days into the database")
          .WithDescription("Walks backward from today (default 14 days, max 31). Skips empty days. Stops on bridge/Garmin errors. Does not run on startup.")
          .Produces<BackfillResult>(StatusCodes.Status200OK);


        group.MapGet("/heartrate", async (DateOnly? date, GarminBridgeService service) =>
        {
            if (ValidateDate(date, out var targetDate) is { } error)
                return error;

            var data = await service.GetHeartRateByDay(targetDate);
            return OkOrNoContent(data);
        }).WithName("GetHeartRateByDay").WithSummary("Get stored heart rate for a specific day")
          .WithDescription("Gets and returns DailyHeartRate and it's HeartRateSamples from DB")
          .Produces<DailyHeartRate>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/stress", async (DateOnly? date, GarminBridgeService service) =>
        {
            if (ValidateDate(date, out var targetDate) is { } error)
                return error;

            var data = await service.GetStressByDay(targetDate);
            return OkOrNoContent(data);
        }).WithName("GetStressByDay").WithSummary("Get stored stress for a specific day")
          .WithDescription("Gets and returns DailyStress from DB")
          .Produces<DailyStress>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/sleep", async (DateOnly? date, GarminBridgeService service) =>
        {
            if (ValidateDate(date, out var targetDate) is { } error)
                return error;

            var data = await service.GetSleepByDay(targetDate);
            return OkOrNoContent(data);
        }).WithName("GetSleepByDay").WithSummary("Get stored sleep for a specific day")
          .WithDescription("Gets and returns DailySleep from DB")
          .Produces<DailySleep>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest);
            group.MapGet("/health", async (GarminBridgeService service) =>
                await service.GarminBridgeHealthCheck() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("GarminBridgeHealthCheck").WithDescription("Checks if the Python GarminConnect bridge server is running");

            return routes;
        }
    }

public sealed record BackfillResult(int Synced, int Empty, DateOnly? StoppedAt, string? Error);
