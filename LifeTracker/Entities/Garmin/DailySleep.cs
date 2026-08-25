namespace LifeTracker.Entities.Garmin;

public class DailySleep : GarminEntity

{
    public int SleepTimeSeconds { get; set; }
    public int DeepSleepSeconds { get; set; }
    public int LightSleepSeconds { get; set; }
    public int RemSleepSeconds { get; set; }
    public int AwakeSleepSeconds { get; set; }

    public double? AvgHeartRate { get; set; }
    public double? AvgSleepStress { get; set; }
}
