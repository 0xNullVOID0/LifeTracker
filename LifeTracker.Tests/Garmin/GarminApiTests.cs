using System.Net;
using Microsoft.Extensions.DependencyInjection;
using static LifeTracker.Tests.Garmin.GarminTestHelpers;

namespace LifeTracker.Tests.Garmin;

public class GarminApiTests(LifeTrackerApiFactory factory) : IClassFixture<LifeTrackerApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    public async Task InitializeAsync()
    {
        // Wipe DB before every test runs to prevent data leakage
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync(); // recreates it fresh after wipe
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetHeartRate_MissingDay_Returns204()
    {
        var res = await _client.GetAsync("/api/garmin/heartrate?date=2020-01-01");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_FutureDate_Returns400()
    {
        var res = await _client.GetAsync("/api/garmin/heartrate?date=2099-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_WithoutJWT_Returns401()
    {
        var anon = factory.CreateClient();
        var res = await anon.GetAsync("/api/garmin/heartrate");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
