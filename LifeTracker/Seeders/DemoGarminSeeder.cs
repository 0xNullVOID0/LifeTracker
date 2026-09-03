using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker.Services;

public static class DemoGarminSeeder
{
    public static async Task SeedIfEmptyAsync(AppDbContext db, ILogger logger)
    {
        if (await db.DailyHeartRates.AnyAsync())
        {
            logger.LogInformation("Skipped Garmin demo seeder since DB already has garmin data");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var days = new List<DateOnly>();

        // add days starting with farthest back to today so data gets properly added from old to new in DB
        for (var i = 6; i >= 0; i--)
            days.Add(today.AddDays(-i));

        foreach (var date in days)
        {
            var offset = today.DayNumber - date.DayNumber;
            var resting = 52 + (6 - offset); 
            var samples = MakeSamples(date, resting);

            db.DailyHeartRates.Add(new DailyHeartRate
            {
                Date = date,
                RestingRate = resting,
                Min = samples.Min(s => s.BPM),
                Max = samples.Max(s => s.BPM),
                Samples = samples
            });

            db.DailyStresses.Add(new DailyStress
            {
                Date = date,
                Average = 14 + (6 - offset),
                Max = 60 + (6 - offset) * 2
            });

            // don't add/create sleep data for today since day is not over yet so (probably/usually) no sleep data
            if (date != today)
            {
                db.DailySleeps.Add(new DailySleep
                {
                    Date = date,
                    SleepTimeSeconds = 25_200 + (6 - offset) * 300,
                    DeepSleepSeconds = 5_400,
                    LightSleepSeconds = 14_400,
                    RemSleepSeconds = 3_600,
                    AwakeSleepSeconds = 1_800,
                    AvgHeartRate = resting,
                    AvgSleepStress = 8 + (6 - offset)
                });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} demo Garmin days ({From} - {To})", days.Count, days[0], days[days.Count - 1]);
    }

    static List<HeartRateSample> MakeSamples(DateOnly date, int resting)
    {
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var random = new Random(date.DayNumber);

        int currentBpm = resting;
        var bpmValues = new int[48];

        // generate heart rate samples 30 min apart(real data is every 2 min) with small variations around the resting rate
        for (int i = 0; i < 48; i++)
        {
            // range of 50-100 bpm but with small steps of -2 to +3 
            currentBpm = Math.Clamp(currentBpm + random.Next(-2, 3), 50, 100);
            bpmValues[i] = currentBpm;
        }

        return bpmValues.Select((b, i) =>
        {
            var timestamp = start.AddMinutes(i * 30);
            bool isSleeping = timestamp.Hour < 7; // set sleep to true for 0-7 am 

            return new HeartRateSample
            {
                Date = date,
                Timestamp = timestamp,
                BPM = b,
                Sleeping = isSleeping
            };
        }).ToList();
    }
}
