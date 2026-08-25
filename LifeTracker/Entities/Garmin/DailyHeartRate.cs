namespace LifeTracker.Entities.Garmin;

public class DailyHeartRate : GarminEntity
{
    public int? RestingRate { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public List<HeartRateSample> Samples { get; set; } = new();
}
