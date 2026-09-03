using LifeTracker.Entities.Buienradar;
using LifeTracker.Services;

namespace LifeTracker.Endpoints;

public static class BuienradarEndpoints
{

    public static IEndpointRouteBuilder MapBuienradarEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/buienradar", async (BuienradarService service) =>
        {
            var data = await service.GetAll();
            return data is not null ? Results.Ok(data) : Results.NoContent();
        }).WithName("GetAllBuienradar").WithTags("Buienradar").WithSummary("Get all Buienradar station measurements from DB")
        .Produces<List<BuienradarStationMeasurement>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        routes.MapPost("/buienradar", async (BuienradarService service) =>
        {
            var data = await service.SyncStationMeasurement();
            return data is not null ? Results.Ok(data) : Results.NoContent();
        }).WithName("SyncBuienradar").WithTags("Buienradar").WithSummary("Sync latest Buienradar station measurement")
        .Produces<BuienradarStationMeasurement>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        return routes;
    }
}
