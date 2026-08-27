using System.ComponentModel;

namespace LifeTracker.Entities.Garmin;

public class DailyHeartRate : GarminEntity
{
    [DefaultValue(55)]
    public int RestingRate { get; set; }

    [DefaultValue(49)]
    public int Min { get; set; }

    [DefaultValue(138)]
    public int Max { get; set; }
    public List<HeartRateSample> Samples { get; set; } = new();
}
