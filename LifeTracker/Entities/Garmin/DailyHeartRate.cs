namespace LifeTracker.Entities.Garmin;

public class DailyHeartRate
{
    public DateOnly Date { get; set; } // primary key
    public int? RestingRate { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public List<HeartRateSample> Samples { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
