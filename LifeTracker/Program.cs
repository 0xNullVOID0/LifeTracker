using System.Text;
using LifeTracker;
using LifeTracker.Configuration;
using LifeTracker.Endpoints;
using LifeTracker.Entities.ESP32;
using LifeTracker.Middleware;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// adds standardized JSON error responses
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient<BuienradarService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:Buienradar"));

builder.Services.AddHttpClient<ESP32Service>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:ESP32RoomClimate"));

builder.Services.Configure<ActivityWatchSettings>(
    builder.Configuration.GetSection(ActivityWatchSettings.SectionName));

builder.Services.AddHttpClient<ActivityWatchService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:ActivityWatch:BaseUrl"));

builder.Services.AddHttpClient<GarminBridgeService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:GarminConnect"));

// get DB credentials and config from appsettings
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30); 
    }));


var app = builder.Build();

builder.WebHost.UseUrls("http://0.0.0.0:5071"); // So ESP32 can reach backend from different IP outside localhost
app.UseMiddleware<DateQueryMiddleware>();
app.MapHealthChecks("/health");
app.MapGarminEndpoints();

// TODO security, authentication for API


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // add scalar for automated API documentation & and easy testing
    app.MapScalarApiReference(options =>
    {
        // tell Scalar where to find the JSON spec using http to fix http/https mismatch when running docker
        options.WithOpenApiRoutePattern("/openapi/v1.json");
    });

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
app.MapGet("/buienradar", async (BuienradarService buienradarService) =>
{
    app.Logger.LogInformation("[API] Route: /buienradar");

    try
    {
        var station_data = await buienradarService.GetBuienradarDataAsync();
        return station_data is not null ? Results.Ok(station_data) : Results.NotFound();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] /buienradar ERROR");
        return Results.Problem($"CRASH: {ex.Message} --- STACKTRACE: {ex.StackTrace}");
    }
})
.WithName("GetBuienradar");

app.MapPost("/API/room-climate", async (RoomClimateMeasurement roomClimate, ESP32Service esp32Service) =>
{
    app.Logger.LogInformation("[API] Route: /API/room-climate ESP32 data received");

    try
    {
        await esp32Service.SaveRoomClimate(roomClimate);

        return Results.Ok(new { success = true, message = "Saved RoomClimateMeasurement measurement" });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "[API] /API/room-climate ERROR");
        return Results.Problem($"CRASH: {ex.Message}");
    }
})
.WithName("PostRoomClimate");

{
    app.Logger.LogInformation("[API] Route: /activity-watch/all");

    try
    {
        var activity_data = await activityWatchService.FetchBucketEvents();
        return activity_data is not null ? Results.Ok(activity_data) : Results.NotFound();
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
        return activity_data is not null ? Results.Ok(activity_data) : Results.NotFound();
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


// apply any pending migrations on startup and check if the database is ready
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int attempts = 10;
    for (var i = 1; i <= attempts; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrated");
            break;
        }
        catch (Npgsql.NpgsqlException ex) when (i < attempts)
        {
            app.Logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Total}). Start Postgres: docker compose up -d db", i, attempts);
            await Task.Delay(2000);
        }
    }
}

// bind default route to scalar too
app.MapGet("/", () => Results.Redirect("/scalar"))
   .ExcludeFromDescription();

app.Run();
