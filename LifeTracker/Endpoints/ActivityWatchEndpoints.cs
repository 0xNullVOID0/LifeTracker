using LifeTracker.Services;

namespace LifeTracker.Endpoints;

public static class ActivityWatchEndpoints
{
    public static IEndpointRouteBuilder MapActivityWatchEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/activity-watch").WithTags("ActivityWatch");

        // optional date parameter expects YYYY-MM-dd format(defaults to today)
        group.MapGet("/new", async (DateOnly? date, ActivityWatchService service) =>
            await service.FetchNewBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("FetchActivityWatchEvents");
        // Optional date parameter expects YYYY-MM-dd format(defaults to today)
        group.MapPost("/sync/all", async (DateOnly? date, ActivityWatchService service) =>
            await service.SyncBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("SyncAllActivityWatchEvents");

        group.MapPost("/sync/new", async (DateOnly? date, ActivityWatchService service) =>
            await service.SyncNewBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("SyncNewActivityWatchEvents");

        return routes;
    }
}
