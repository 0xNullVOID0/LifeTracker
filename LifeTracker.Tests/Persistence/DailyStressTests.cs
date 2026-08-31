using LifeTracker;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeTracker.Tests.Persistence;

public class DailyStressTests
{
    static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    static GarminBridgeService CreateService(AppDbContext db) =>
        new(new HttpClient { BaseAddress = new Uri("http://127.0.0.1/") }, db, NullLogger<GarminBridgeService>.Instance);

    [Fact]
    public async Task SaveDailyStress_TwiceSameDate_UpdatesSingleRow()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailyStress(new DailyStress { Date = date, Average = 20, Max = 50 });
        await service.SaveDailyStress(new DailyStress { Date = date, Average = 25, Max = 80 });

        var rows = await db.DailyStresses.ToListAsync();

        // Verify Upsert went properly by checking that only 1 row exists and that it's values have been updated
        Assert.Single(rows);
        Assert.Equal(25, rows[0].Average);
        Assert.Equal(80, rows[0].Max);
    }

    [Fact]
    public async Task GetStressByDay_WhenMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetStressByDay(new DateOnly(2020, 1, 1));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStressByDay_WhenSaved_ReturnsRow()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailyStress(new DailyStress { Date = date, Average = 17, Max = 87 });

        var result = await service.GetStressByDay(date);

        Assert.NotNull(result);
        Assert.Equal(17, result.Average);
        Assert.Equal(87, result.Max);
    }
}
