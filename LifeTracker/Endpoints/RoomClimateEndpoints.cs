using LifeTracker.Entities.ESP32;
using LifeTracker.Filters;
using LifeTracker.Infrastructure;
using LifeTracker.Services;
using static LifeTracker.Endpoints.EndpointHelpers;


namespace LifeTracker.Endpoints;

public static class RoomClimateEndpoints
{
    public static IEndpointRouteBuilder MapRoomClimateEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/room-climate").WithTags("RoomClimate");

        group.MapGet("/", async (DateTimeOffset? start, DateTimeOffset? end, ESP32Service service, AppClock clock) =>
            {
                DateTimeOffset from;
                DateTimeOffset to;

                // TODO move to date/range filter for global use
                if (!start.HasValue && !end.HasValue)
                    return OkOrNoContent(await service.GetByDay(clock.Today()));
                else if (start.HasValue && !end.HasValue)
                {
                    from = start.Value.ToUniversalTime();
                    to = clock.StartOfLocalDay(clock.ToLocalDate(start.Value).AddDays(1));
                }
                else if (!start.HasValue && end.HasValue)
                {
                    from = clock.StartOfLocalDay(clock.ToLocalDate(end.Value));
                    to = end.Value.ToUniversalTime();
                }
                else
                {
                    from = start.Value.ToUniversalTime();
                    to = end.Value.ToUniversalTime();
                }

                if (from > to)
                    return Results.BadRequest(new { error = "start must be before or equal to end." });

                return OkOrNoContent(await service.GetRange(from, to));
            }).WithName("GetRoomClimate")
            .WithSummary("Get room climate measurements")
            .WithDescription("Get measurements by optional start/end range or defaults to today without any parameters.")
            .Produces<List<RoomClimateMeasurement>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/day", async (DateOnly? date, ESP32Service service) =>
                OkOrNoContent(await service.GetByDay(DateQueryFilter.Resolved(date))))
            .AddEndpointFilter(DateQueryFilter.ResolveDate)
            .WithName("GetRoomClimateByDay")
            .WithSummary("Get room climate measurements for a specific day")
            .WithDescription("Returns measurements for the given day")
            .Produces<List<RoomClimateMeasurement>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        // TODO fix handling route getting bad, wrong or not filled in json values
        group.MapPost("/", async (RoomClimateMeasurement body, ESP32Service service, IHostEnvironment env) =>
        {
            await service.SaveRoomClimate(body);
            return Results.Ok(new { success = true });                                                                                            
        }).WithName("PostRoomClimate").WithSummary("Ingest room climate measurements from ESP32 sensors")
        .AllowAnonymous()
        .Produces<RoomClimateMeasurement>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .AddEndpointFilter(DeviceKeyFilter.Require); // ESP32 uses device key instead of JWT for auth


        return routes;
    }
}
