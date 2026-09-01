using LifeTracker.Entities.Buienradar;
using LifeTracker.Services;

namespace LifeTracker.Endpoints;

public static class BuienradarEndpoints
{

    public static IEndpointRouteBuilder MapBuienradarEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/buienradar", async (BuienradarService service) =>
        {
            var data = await service.SyncStationMeasurement();
            return data is not null ? Results.Ok(data) : Results.NoContent();
        }).WithName("GetBuienradar").WithTags("Buienradar").WithSummary("Sync latest Buienradar station measurement")
        .Produces<BuienradarStationMeasurement>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        return routes;
    }
}
