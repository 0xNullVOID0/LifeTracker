using LifeTracker;
using LifeTracker.Configuration;
using LifeTracker.Endpoints;
using LifeTracker.Middleware;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

//AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
//{
//    // catch every exception to print in console
//    Console.WriteLine($"[Internal Exception] {eventArgs.Exception.GetType().Name}: {eventArgs.Exception.Message}");
//};

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// adds standardized JSON error responses
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

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

app.MapHealthChecks("/health");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // add scalar for automated API documentation & and easy testing

    app.UseDeveloperExceptionPage(); // gives full stack traces for debugging

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = "https://localhost:5071/scalar";
        Console.WriteLine($"\n→ Scalar UI: {url}\n");
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();

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


app.Logger.LogInformation("\n\n--------------------------------------------------");
app.Logger.LogInformation("LifeTracker API started");
app.Logger.LogInformation("--------------------------------------------------\n");

app.Run();