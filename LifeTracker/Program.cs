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


// Set every route to auth JWT protected by default, .AllowAnonymous() on the unprotected exception routes
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Register bearer scheme and only show auth required on protected routes
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerOperationTransformer>();
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient<BuienradarService>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:Buienradar"));
builder.Services.AddHostedService<BuienradarBackgroundService>();

builder.Services.AddHttpClient<ESP32Service>(client =>
    client.BaseAddress = builder.Configuration.GetRequiredUri("APIs:ESP32RoomClimate"));

builder.Services.Configure<ActivityWatchOptions>(
    builder.Configuration.GetSection(ActivityWatchOptions.SectionName));

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
{
    app.UseExceptionHandler();
    app.UseHttpsRedirection(); // only use HTTPS in production
}


app.UseHttpsRedirection();
app.UseMiddleware<DateQueryMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapBuienradarEndpoints();
app.MapRoomClimateEndpoints();
app.MapActivityWatchEndpoints();
app.MapGarminEndpoints();



if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Demo"))
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
        var url = "http://0.0.0.0:5071/scalar";
        Console.WriteLine($"\nView and test all endpoints in Scalar UI: {url}\n");
    });
}



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

    if (db.Database.IsRelational()) // a check for more extensive integration tests later
    {
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

    await DemoGarminSeeder.SeedIfEmptyAsync(db, app.Logger);

    if (app.Environment.IsEnvironment("Demo"))
    {
        await DemoGarminSeeder.SeedIfEmptyAsync(db, app.Logger);
    }
}


// bind default route to scalar too
app.MapGet("/", () => Results.Redirect("/scalar")).AllowAnonymous().ExcludeFromDescription();

app.Run();

