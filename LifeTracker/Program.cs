using LifeTracker;
//AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
//{
//    // catch every exception to print in console
//    Console.WriteLine($"[Internal Exception] {eventArgs.Exception.GetType().Name}: {eventArgs.Exception.Message}");
//};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<WeatherService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/buienradar", async (WeatherService weatherService) =>
{
    app.Logger.LogInformation("[API] Route: /buienradar");

    try
    {
        var station_data = await weatherService.GetBuienradarDataAsync();
        return station_data != null ? Results.Ok(station_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] buienradar ERROR");
        Console.WriteLine(ex.ToString()); 

        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("GetBuienradar");


app.MapGet("/test", async () =>
{
    app.Logger.LogInformation("[API] Route: /test");
    Console.WriteLine($"TEST TEST");
})
.WithName("test");



app.Logger.LogInformation("\n\n--------------------------------------------------");
app.Logger.LogInformation("LifeTracker API started");
app.Logger.LogInformation("Buienradar endpoint: http://127.0.0.1:5070/buienradar");
app.Logger.LogInformation("Buienradar endpoint: https://127.0.0.1:5071/buienradar");
app.Logger.LogInformation("--------------------------------------------------\n");

app.Run();