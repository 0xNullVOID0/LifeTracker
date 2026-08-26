namespace LifeTracker.Entities.Garmin;

public class GarminDay : GarminEntity
{
    public DailyHeartRate HeartRate { get; set; }
    public DailyStress Stress { get; set; }
    public DailySleep Sleep { get; set; }

    public GarminDay(DailyHeartRate heartRate, DailyStress stress, DailySleep sleep)
    {
        Date = heartRate.Date;
        HeartRate = heartRate;
        Stress = stress;
        Sleep = sleep;
    }
}
