using LifeTracker;
using LifeTracker.Configuration;
using Microsoft.EntityFrameworkCore;

//AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
//{
//    // catch every exception to print in console
//    Console.WriteLine($"[Internal Exception] {eventArgs.Exception.GetType().Name}: {eventArgs.Exception.Message}");
//};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<WeatherService>(client => 
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:Buienradar"));


builder.Services.Configure<ActivityWatchSettings>(
    builder.Configuration.GetSection(ActivityWatchSettings.SectionName));

builder.Services.AddHttpClient<ActivityWatchService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:ActivityWatch:BaseUrl"));

builder.Services.AddHttpClient<GarminBridgeService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:GarminConnect"));

// get DB credentials and config from appsettings
var connectionString = builder.Configuration.GetConnectionString("LifeTrackerDB")
    ?? throw new InvalidOperationException("Connection string 'LifeTrackerDB' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30); 
    }));


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
        app.Logger.LogError(ex, "[API] /buienradar ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("GetBuienradar");

app.MapGet("/activity-watch/all", async (ActivityWatchService activityWatchService) =>
{
    app.Logger.LogInformation("[API] Route: /activity-watch/all");

    try
    {
        var activity_data = await activityWatchService.FetchBucketEvents();
        return activity_data != null ? Results.Ok(activity_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] activity-watch/all ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("FetchAllActivityWatchEvents");



app.MapGet("/activity-watch/new", async (ActivityWatchService activityWatchService) =>
{
    app.Logger.LogInformation("[API] Route: /activity-watch/new");

    try
    {
        var activity_data = await activityWatchService.FetchNewBucketEvents();
        return activity_data != null ? Results.Ok(activity_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] activity-watch/new ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("FetchNewActivityWatchEvents");


app.MapGet("/garmin/heartrate", async (GarminBridgeService garminBridgeService) =>
{
    app.Logger.LogInformation("[API] Route: /garmin/heartrate");

    try
    {
        var heartrate_data = await garminBridgeService.FetchTodaysHeartRate();
        return heartrate_data != null ? Results.Ok(heartrate_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] /garmin/heartrate ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("FetchTodaysHeartRates");


app.MapGet("/garmin/stress", async (GarminBridgeService garminBridgeService) =>
{
    app.Logger.LogInformation("[API] Route: /garmin/stress");

    try
    {
        var heartrate_data = await garminBridgeService.FetchTodaysStressLevel();
        return heartrate_data != null ? Results.Ok(heartrate_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] /garmin/stress ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("FetchTodaysStressLevel");



app.MapGet("/test", async () =>
{
    app.Logger.LogInformation("[API] Route: /test");
    Console.WriteLine($"TEST TEST");
})
.WithName("test");



app.Logger.LogInformation("\n\n--------------------------------------------------");
app.Logger.LogInformation("LifeTracker API started");
app.Logger.LogInformation("Buienradar endpoint: https://127.0.0.1:5071/buienradar");
app.Logger.LogInformation("FetchAllActivityWatchEvents endpoint: https://127.0.0.1:5071/activity-watch/all");
app.Logger.LogInformation("FetchNewActivityWatchEvents endpoint: https://127.0.0.1:5071/activity-watch/new");
app.Logger.LogInformation("--------------------------------------------------\n");

app.Run();