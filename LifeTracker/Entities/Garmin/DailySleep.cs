namespace LifeTracker.Entities.Garmin;

public class DailySleep
{
    public DateOnly Date { get; set; }   // PK

    public int SleepTimeSeconds { get; set; }
    public int DeepSleepSeconds { get; set; }
    public int LightSleepSeconds { get; set; }
    public int RemSleepSeconds { get; set; }
    public int AwakeSleepSeconds { get; set; }

    public double? AvgHeartRate { get; set; }
    public double? AvgSleepStress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
