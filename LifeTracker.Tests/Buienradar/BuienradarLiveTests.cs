using LifeTracker;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker.Tests.Buienradar;

// Test BuienradarService with live data from Buienradar's API and check if the data is saved correctly in the in memory DB.
public class BuienradarLiveTests
{
    static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    static BuienradarService CreateService(AppDbContext db)
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("https://data.buienradar.nl/2.0/feed/json"),
            Timeout = TimeSpan.FromSeconds(20)
        };
        return new BuienradarService(http, db);
    }

    [Fact]
    public async Task SyncStationMeasurement_LiveFeed_SavesHeinoStation()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var station = await service.SyncStationMeasurement();

        // Check if we even received a station and its data back, if its the right(heino/6278) and has reasonable and proper data/measurements
        Assert.NotNull(station);
        Assert.True(station.StationId == 6278 || station.StationName.Contains("Heino", StringComparison.OrdinalIgnoreCase), 
            $"Unexpected station: {station.StationId} {station.StationName}");
        Assert.InRange(station.Temperature, -30f, 50f);

        // Check if data was properly saved in the in memory DB
        var saved = await db.BuienradarStationMeasurements.ToListAsync();
        Assert.Single(saved);
        Assert.Equal(station.StationId, saved[0].StationId);
    }
}
