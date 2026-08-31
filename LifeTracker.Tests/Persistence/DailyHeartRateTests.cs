using LifeTracker;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeTracker.Tests.Persistence;

public class DailyHeartRateTests
{
    static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    static GarminBridgeService CreateService(AppDbContext db) =>
        new(new HttpClient { BaseAddress = new Uri("http://127.0.0.1/") }, db, NullLogger<GarminBridgeService>.Instance);

    static DailyHeartRate Day(DateOnly date, int resting, params int[] bpms) => new()
    {
        Date = date,
        RestingRate = resting,
        Min = bpms.Min(),
        Max = bpms.Max(),
        Samples = bpms.Select((bpm, i) => new HeartRateSample
        {
            Date = date,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1787063520000 + i * 120_000), // create +2 min incremented timestamps for each sample just like official API
            BPM = bpm
        }).ToList()
    };

    [Fact]
    public async Task GetHeartRateByDay_WhenSaved_IncludesSamples()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailyHeartRate(Day(date, 55, 62, 64));

        var result = await service.GetHeartRateByDay(date);

        Assert.NotNull(result);
        Assert.Equal(55, result.RestingRate);
        Assert.Equal(2, result.Samples.Count);
    }

    [Fact]
    public async Task SaveDailyHeartRate_TwiceSameDate_ReplacesSamples()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailyHeartRate(Day(date, 50, [56, 58]));
        await service.SaveDailyHeartRate(Day(date, 55, [62, 64]));


        // Verify Upsert went properly by checking that only 1 DailyHeartRate row exists and that it's values have been updated
        // Also check if related HeartRateSample rows have been replaced instead of duplicated
        var saved = await db.DailyHeartRates.Include(d => d.Samples).SingleAsync();
        Assert.Equal(55, saved.RestingRate);
        Assert.Equal(2, saved.Samples.Count); // should only be 2 instead of 4 since the previous with same timestamps get replaced
        Assert.DoesNotContain(saved.Samples, s => s.BPM == 56);
        Assert.Contains(saved.Samples, s => s.BPM == 62);
    }

    [Fact]
    public async Task SaveDailyHeartRate_LaterInDay_GrowsSampleList()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var date = new DateOnly(2026, 8, 18);

        await service.SaveDailyHeartRate(Day(date, 50, [56, 58]));
        await service.SaveDailyHeartRate(Day(date, 55, [56, 58, 62, 64]));

        // Verify Upsert of related HeartRateSample rows has grown the list, that old Range has been removed and replaced with the newer longer values list
        var saved = await db.DailyHeartRates.Include(d => d.Samples).SingleAsync();
        Assert.Equal(55, saved.RestingRate);
        Assert.Equal(4, saved.Samples.Count);
        Assert.Contains(saved.Samples, s => s.BPM == 56);
        Assert.Contains(saved.Samples, s => s.BPM == 64);
    }


    [Fact]
    public async Task GetHeartRateByDay_WhenMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetHeartRateByDay(new DateOnly(2020, 1, 1));

        Assert.Null(result);
    }
}
