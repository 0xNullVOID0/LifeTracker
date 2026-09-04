namespace LifeTracker.Endpoints;

public static class EndpointHelpers
{
    // TODO more overloads and more appropriate REST status codes for different scenarios
    public static IResult OkOrNoContent<T>(T? data) =>
        data is not null ? Results.Ok(data) : Results.NoContent();

    public static RouteHandlerBuilder ConfigureRoute<T>(this RouteHandlerBuilder builder, string name, string summary, string description) =>
        builder.WithName(name).WithSummary(summary).WithDescription(description)
               .Produces<T>(StatusCodes.Status200OK).Produces(StatusCodes.Status204NoContent)
               .ProducesProblem(StatusCodes.Status400BadRequest);
    public static bool IsFuture(DateOnly date) => date > DateOnly.FromDateTime(DateTime.Today);

    public static IResult? ValidateDate(DateOnly? date, out DateOnly targetDate)
    {
        targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        if (IsFuture(targetDate))
            return Results.BadRequest(new { error = "Cannot request non existent data from future dates." });
        return null; // means OK(no errors found)
    }
}
