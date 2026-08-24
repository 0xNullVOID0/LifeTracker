namespace LifeTracker.Entities.Garmin;

// TOOD rename to DailyStressLevel?
public class DailyStress
{
    public DateOnly Date { get; set; } // primary key
    public int? Average { get; set; }
    public int? Max { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
