using System.Net.Http.Headers;
using LifeTracker.Configuration;
using LifeTracker.Infrastructure;
using LifeTracker.Services;
using LifeTracker.Tests.Garmin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LifeTracker.Tests;

public sealed class LifeTrackerApiFactory : WebApplicationFactory<Program>
{
    public HttpMessageHandler GarminBridge { get; } = new BridgeHandler();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // set and config test env
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Key"] = TestJWT.Options.Key,
                ["JWT:Issuer"] = TestJWT.Options.Issuer,
                ["JWT:Audience"] = TestJWT.Options.Audience,
                ["JWT:Password"] = TestJWT.Options.Password,
                
                // set values so Program.cs doesn't crash in test env
                ["ConnectionStrings:DefaultConnection"] = "Host=unused",
                ["APIs:GarminConnect"] = "http://garmin-bridge/",
                ["APIs:Buienradar"] = "http://buienradar-mock/",
                ["APIs:ESP32RoomClimate"] = "http://esp32-mock/",
                ["APIs:ActivityWatch:BaseUrl"] = "http://activitywatch-mock/"
            }));

        builder.ConfigureTestServices(services =>
        {
            // drop normal Program.cs Npgsql DB and replace with test version
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("api-tests"));

            services.AddHttpClient<GarminBridgeService>().ConfigurePrimaryHttpMessageHandler(() => GarminBridge);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        string token = JwtTokenGenerator.Generate(TestJWT.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public static class TestJWT
{
    public static readonly JwtOptions Options = new()
    {
        Key = "TestingKeyVcxnELfB5GI9K93KxywRZTMwpaCngXfZ", Issuer = "test", Audience = "test", Password = "test"
    };
}
