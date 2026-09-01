using LifeTracker.Entities.ESP32;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;

namespace LifeTracker.Endpoints;

public static class RoomClimateEndpoints
{
    public static IEndpointRouteBuilder MapRoomClimateEndpoints(this IEndpointRouteBuilder routes)
    {
        // TODO fix handling route getting bad, wrong or not filled in json values
        routes.MapPost("/room-climate", async (RoomClimateMeasurement body, ESP32Service service) =>
        {
            await service.SaveRoomClimate(body);
            return Results.Ok(new { success = true });                                                                                            
        }).WithName("PostRoomClimate").WithTags("RoomClimate").WithSummary("Ingest room climate measurements from ESP32 sensors")
        // TODO use device key instead of JWT for esp32?
        .AllowAnonymous() // easier for now to not have ESP32 deal with JWT header tokens 
        .Produces<RoomClimateMeasurement>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);


        return routes;
    }
}
