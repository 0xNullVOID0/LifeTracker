using LifeTracker.Entities.ESP32;
using LifeTracker.Filters;
using LifeTracker.Services;
using static LifeTracker.Endpoints.EndpointHelpers;


namespace LifeTracker.Endpoints;

public static class RoomClimateEndpoints
{
    public static IEndpointRouteBuilder MapRoomClimateEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/room-climate", async (ESP32Service service) => 
                OkOrNoContent(await service.GetAll())).WithName("GetAllRoomClimate").WithTags("RoomClimate")
            .WithSummary("Get all room climate measurements from DB")
            .Produces<List<RoomClimateMeasurement>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        // TODO fix handling route getting bad, wrong or not filled in json values
        routes.MapPost("/room-climate", async (RoomClimateMeasurement body, ESP32Service service, IHostEnvironment env) =>
        {
            if (env.IsEnvironment("Demo"))
                return Results.Json(new { error = "Room climate ingest is disabled in Demo" }, statusCode: StatusCodes.Status503ServiceUnavailable);

            await service.SaveRoomClimate(body);
            return Results.Ok(new { success = true });                                                                                            
        }).WithName("PostRoomClimate").WithTags("RoomClimate").WithSummary("Ingest room climate measurements from ESP32 sensors")
        .AllowAnonymous()
        .Produces<RoomClimateMeasurement>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AddEndpointFilter(DeviceKeyFilter.Require); // ESP32 uses device key instead of JWT for auth


        return routes;
    }
}
