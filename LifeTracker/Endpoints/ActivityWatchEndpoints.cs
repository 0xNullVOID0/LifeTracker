using LifeTracker.Services;

namespace LifeTracker.Endpoints
{
    public static class ActivityWatchEndpoints
    {
        public static IEndpointRouteBuilder MapActivityWatchEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/activity-watch").WithTags("ActivityWatch");

            // optional date parameter expects YYYY-MM-dd format(defaults to today)
            group.MapGet("/new", async (DateOnly? date, ActivityWatchService service) =>
                await service.FetchNewBucketEvents() is { } data ? Results.Ok(data) : Results.NotFound()).WithName("FetchActivityWatchEvents");

            return routes;
        }
    }
}
