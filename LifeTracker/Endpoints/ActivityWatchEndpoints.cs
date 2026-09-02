using LifeTracker.Services;

namespace LifeTracker.Endpoints;

public static class ActivityWatchEndpoints
{
    public static IEndpointRouteBuilder MapActivityWatchEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/activity-watch").WithTags("ActivityWatch");

        // TODO handle fucked start and end date things again, add to middleware thing 
        // Fetch events from own DB with optional start and or end range by timestamp
        group.MapGet("/", async (DateTimeOffset? start, DateTimeOffset? end, ActivityWatchService service) =>
        {
            var data = start.HasValue ? await service.GetBucketEvents(start.Value, end) : await service.GetBucketEvents();
            return data is not null ? Results.Ok(data) : Results.NotFound();
        }).WithName("GetActivityWatchEvents");

        // Optional date parameter expects YYYY-MM-dd format(defaults to today)
        group.MapPost("/sync/all", async (DateOnly? date, ActivityWatchService service) =>
            await service.SyncBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("SyncAllActivityWatchEvents");

        group.MapPost("/sync/new", async (DateOnly? date, ActivityWatchService service) =>
            await service.SyncNewBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("SyncNewActivityWatchEvents");

        return routes;
    }
}
