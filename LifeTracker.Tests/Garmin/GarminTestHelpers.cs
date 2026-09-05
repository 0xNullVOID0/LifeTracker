using System.Text;
using LifeTracker.Entities.Garmin;
using Microsoft.Extensions.DependencyInjection;

namespace LifeTracker.Tests.Garmin;

public static class GarminTestHelpers
{
    public static async Task SeedHeartRateAsync(LifeTrackerApiFactory factory, DateOnly date, int resting = 60)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.DailyHeartRates.Add(new DailyHeartRate
        {
            Date = date,
            RestingRate = resting,
            Min = resting - 5,
            Max = resting + 20,
            Samples =
            [
                new HeartRateSample
                {
                    Date = date, Timestamp = date.ToDateTime(TimeOnly.MinValue), BPM = resting, Sleeping = false
                }
            ]
        });
        await db.SaveChangesAsync();
    }
    
    public static StringContent CreateJsonContent(string file, string folder = "Garmin/JSON")
    {
        var jsonPath = Path.Combine([AppContext.BaseDirectory, folder, file]);
        var jsonContent = File.ReadAllText(jsonPath);
    
        return new StringContent(jsonContent, Encoding.UTF8, "application/json");
    }
}
