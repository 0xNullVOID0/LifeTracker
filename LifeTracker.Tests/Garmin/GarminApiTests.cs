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

    // TODO add sync tests
    // TODO add timezone midnight few minute difference tests
    // TODO add json body check tests instead of only checking status code

    [Fact]
    public async Task SyncStress_Returns200()
    {
        var date = new DateOnly(2026, 8, 20);
        
        BridgeHandler.Responses["health"] = new HttpResponseMessage(HttpStatusCode.OK);
        
        // mock for external python garmin bridge
        BridgeHandler.Responses[$"stress?date={date:yyyy-MM-dd}"] = 
            new HttpResponseMessage(HttpStatusCode.OK) { 
                Content = CreateJsonContent("stress.json")
            };

        var res = await _client.PostAsync($"/api/garmin/sync/stress?date={date:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
    
    [Fact]
    public async Task SyncStress_ValidJSON_Returns200()
    {
        var date = new DateOnly(2026, 9, 4);
        
        // mock for external python bridge health check and stress route
        BridgeHandler.Responses["health"] = new HttpResponseMessage(HttpStatusCode.OK);
        BridgeHandler.Responses[$"stress?date={date:yyyy-MM-dd}"] = 
            new HttpResponseMessage(HttpStatusCode.OK) { 
                Content = CreateJsonContent("stress.json")
            };

        var res = await _client.PostAsync($"/api/garmin/sync/stress?date={date:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        
        // check 
        var resBody = await res.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(resBody));
        
    }
    
    #region /heartrate
    [Fact]
    public async Task GetHeartRate_ExistingDate_Returns200()
    {
        var date = new DateOnly(2026, 8, 20);
        await SeedHeartRateAsync(factory, date);

        var res = await _client.GetAsync($"/api/garmin/heartrate?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_NoDateParam_DefaultsToToday_WithExistingData_Returns200()
    {
        var datetime = DateTime.Today;
        var date = DateOnly.FromDateTime(datetime);
        await SeedHeartRateAsync(factory, date);

        var res = await _client.GetAsync($"/api/garmin/heartrate");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_ExistingData_WrongDate_Returns204()
    {
        var date = new DateOnly(2026, 8, 20);
        await SeedHeartRateAsync(factory, date);

        date = date.AddDays(1);
        var res = await _client.GetAsync($"/api/garmin/heartrate?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_NoDateParam_WithoutData_Returns204()
    {
        var res = await _client.GetAsync("/api/garmin/heartrate");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }


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
    public async Task GetHeartRate_FaultyDateParam_Returns400()
    {
        var res = await _client.GetAsync("/api/garmin/heartrate?date=aaa");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_ImpossibleDateParam_Returns400()
    {
        // queries non existent february 30th
        var res = await _client.GetAsync("/api/garmin/heartrate?date=2026-02-30");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_13thMonth_Returns400()
    {
        var res = await _client.GetAsync("/api/garmin/heartrate?date=2026-13-13");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetHeartRate_WithoutJWT_Returns401()
    {
        var anon = factory.CreateClient();
        var res = await anon.GetAsync("/api/garmin/heartrate");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
    #endregion
}
