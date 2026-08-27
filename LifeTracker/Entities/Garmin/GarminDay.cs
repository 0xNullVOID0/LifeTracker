namespace LifeTracker.Entities.Garmin;

public class GarminDay : GarminEntity
{
    public DailyHeartRate HeartRate { get; set; }
    public DailyStress Stress { get; set; }
    public DailySleep? Sleep { get; set; } // sleep is really the only nullable/optional property since you may not have sleep data for the given day yet

    public GarminDay(DateOnly date, DailyHeartRate heartRate, DailyStress stress, DailySleep sleep)
    {
        Date = date; 
        HeartRate = heartRate;
        Stress = stress;
        Sleep = sleep;
    }
}
