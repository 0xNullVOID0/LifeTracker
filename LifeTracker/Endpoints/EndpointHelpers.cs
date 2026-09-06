namespace LifeTracker.Endpoints;

public static class EndpointHelpers
{
    // TODO more overloads and more appropriate REST status codes for different scenarios
    public static IResult OkOrNoContent<T>(T? data) =>
        data is not null ? Results.Ok(data) : Results.NoContent();

    public static RouteHandlerBuilder ConfigureRoute<T>(this RouteHandlerBuilder builder, string name, string summary, string description) =>
        builder.WithName(name).WithSummary(summary).WithDescription(description)
            .Produces<T>(StatusCodes.Status200OK).Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
}
