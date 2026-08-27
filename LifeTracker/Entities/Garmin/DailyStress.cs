using System.ComponentModel;

namespace LifeTracker.Entities.Garmin;

// TOOD rename to DailyStressLevel?
public class DailyStress : GarminEntity
{
    [DefaultValue(16)]
    public int Average { get; set; }

    [DefaultValue(99)]
    public int Max { get; set; }
}
