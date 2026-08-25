namespace LifeTracker.Entities.Garmin;

// TOOD rename to DailyStressLevel?
public class DailyStress : GarminEntity
{
    public int? Average { get; set; }
    public int? Max { get; set; }
}
