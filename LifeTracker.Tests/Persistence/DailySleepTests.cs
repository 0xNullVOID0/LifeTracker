using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeTracker.Tests.Persistence;

public class DailySleepTests
{
    static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    static GarminBridgeService CreateService(AppDbContext db) =>
        new(new HttpClient { BaseAddress = new Uri("http://127.0.0.1/") }, db,
            NullLogger<GarminBridgeService>.Instance);

    static DailySleep Sleep(DateOnly date, int total, int stress) => new()
    {
        Date = date,
        SleepTimeSeconds = total,
        DeepSleepSeconds = 1000,
        LightSleepSeconds = 2000,
        RemSleepSeconds = 500,
        AwakeSleepSeconds = 200,
        AvgHeartRate = 55,
        AvgSleepStress = stress
    };

    [Fact]
    public async Task SaveDailySleep_TwiceSameDate_UpdatesSingleRow()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        // TODO add tests for associated HeartRateSamples too 
        await service.SaveDailySleep(Sleep(date, 20000, 8), []);
        await service.SaveDailySleep(Sleep(date, 22920, 11), []);

        // Verify Upsert went properly by checking that only 1 row exists and that it's values have been updated
        var rows = await db.DailySleeps.ToListAsync();
        Assert.Single(rows);
        Assert.Equal(22920, rows[0].SleepTimeSeconds);
        Assert.Equal(11, rows[0].AvgSleepStress);
    }

    [Fact]
    public async Task GetSleepByDay_WhenMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetSleepByDay(new DateOnly(2020, 1, 1));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSleepByDay_WhenSaved_ReturnsRow()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailySleep(Sleep(date, 22920, 11), []);

        var result = await service.GetSleepByDay(date);

        Assert.NotNull(result);
        Assert.Equal(22920, result.SleepTimeSeconds);
        Assert.Equal(55, result.AvgHeartRate);
        Assert.Equal(11, result.AvgSleepStress);
    }
}
