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

        // Check if we even received a station and its data back and if its the right one(heino/6278) 
        Assert.NotNull(station);
        Assert.False(string.IsNullOrWhiteSpace(station.StationName));
        Assert.True(station.StationId == 6278 || station.StationName.Contains("Heino", StringComparison.OrdinalIgnoreCase),
            $"Unexpected station: {station.StationId} {station.StationName}");


        // Check if measurements are within proper, reasonable range
        Assert.InRange(station.Temperature, -30f, 50f);
        Assert.InRange(station.Humidity, 0f, 100f);

        // Heino station does not have air pressure sensor so it should be null
        Assert.Null(station.AirPressure);

        // TODO some of these could be more specific but for now we just check if they are not null or empty and if the numbers are not negative

        // Null checks
        if (station.WeatherDescription is not null)
            Assert.False(string.IsNullOrWhiteSpace(station.WeatherDescription));

        if (station.WindDirection is not null)
            Assert.False(string.IsNullOrWhiteSpace(station.WindDirection));

        if (station.Precipitation is { } rain)
            Assert.True(rain >= 0f, $"precipitation {rain}");

        if (station.RainFallLastHour is { } hour)
            Assert.True(hour >= 0f, $"rain last hour {hour}");

        if (station.RainFallLast24Hour is { } day)
            Assert.True(day >= 0f, $"rain last 24h {day}");

        if (station.WindspeedBft is { } bft)
            Assert.InRange(bft, 0f, 12f);

        if (station.SunPower is { } sun)
            Assert.True(sun >= 0f, $"sunpower {sun}");


        // Check if data was properly saved in the in memory DB
        var saved = await db.BuienradarStationMeasurements.SingleAsync();
        Assert.Equal(station.StationId, saved.StationId);
        Assert.Equal(station.Temperature, saved.Temperature);
        Assert.Equal(station.Humidity, saved.Humidity);
    }
}
