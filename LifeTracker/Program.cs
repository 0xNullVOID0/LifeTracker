using System.Text;
using LifeTracker;
using LifeTracker.Configuration;
using LifeTracker.Endpoints;
using LifeTracker.Entities.ESP32;
using LifeTracker.Infrastructure;
using LifeTracker.Middleware;
using LifeTracker.Services;
using LifeTracker.Services.Background;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

builder.WebHost.UseUrls("http://0.0.0.0:5071"); // So ESP32 can reach backend from different IP outside localhost



builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
var JWT = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT section missing in appsettings");

if (string.IsNullOrWhiteSpace(JWT.Key))
    throw new InvalidOperationException("JWT:Key missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = JWT.Issuer,
            ValidAudience = JWT.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT.Key))
        };
    });


// Set every route to auth JWT protected by default, .AllowAnonymous() on the exception routes for no auth required
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Add and show auth routes and bearer headers to OpenAPI Scalar UI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// adds standardized JSON error responses
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient<BuienradarService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:Buienradar"));
builder.Services.AddHostedService<BuienradarBackgroundService>();

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

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler();


app.UseHttpsRedirection();
app.UseMiddleware<DateQueryMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGarminEndpoints();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous(); 
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");

        // Set Bearer options for OpenAPI to use JWT in Scalar UI
        options.AddPreferredSecuritySchemes("Bearer");
        options.AddHttpAuthentication("Bearer", auth =>
        {
            auth.Token = JwtTokenGenerator.Generate(JWT);
    });
    }).AllowAnonymous();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = "http://localhost:5071/scalar";
        Console.WriteLine($"\n→ Scalar UI: {url}\n");
    });
}

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
// TODO use device key instead of JWT for esp32?
.WithName("PostRoomClimate").AllowAnonymous(); // easier for now to not have ESP32 deal with JWT header tokens

app.MapGet("/activity-watch/all", async (ActivityWatchService activityWatchService) =>
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

app.MapPost("/api/auth/token", (TokenRequest request) =>
{
    var password = JWT.Password;
    if (string.IsNullOrWhiteSpace(password) || request.Password != password)
        return Results.Unauthorized();

    var token = JwtTokenGenerator.Generate(JWT);
    return Results.Ok(new { token });
}).AllowAnonymous().WithName("GenerateToken").WithSummary("Get JWT with demo/master password");

//return Results.Ok(new { token = JwtTokenGenerator.GenerateToken(config, "Demo") });



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
app.MapGet("/", () => Results.Redirect("/scalar")).AllowAnonymous().ExcludeFromDescription();

app.Run();

