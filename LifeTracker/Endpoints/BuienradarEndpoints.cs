using LifeTracker.Entities.Buienradar;
using LifeTracker.Services;
using static LifeTracker.Endpoints.EndpointHelpers;


namespace LifeTracker.Endpoints;

public static class BuienradarEndpoints
{

    public static IEndpointRouteBuilder MapBuienradarEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/buienradar", async (BuienradarService service) =>
        {
            var data = await service.GetAll();
            return OkOrNoContent(data);
        }).WithName("GetAllBuienradar").WithTags("Buienradar").WithSummary("Get all Buienradar station measurements from DB")
        .Produces<List<BuienradarStationMeasurement>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        routes.MapPost("/buienradar", async (BuienradarService service) =>
        {
            var data = await service.SyncStationMeasurement();
            return OkOrNoContent(data);
        }).WithName("SyncBuienradar").WithTags("Buienradar").WithSummary("Sync latest Buienradar station measurement").WithDescription("Fetches the configured weather station(currently Heino) from their feed and stores it to DB")
        .Produces<BuienradarStationMeasurement>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status204NoContent);

        return routes;
    }
}
