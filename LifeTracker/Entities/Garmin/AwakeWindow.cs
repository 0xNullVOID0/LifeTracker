using System.ComponentModel;

namespace LifeTracker.Entities.Garmin;

public class AwakeWindow(DailySleep priorSleep, DailySleep subsequentSleep)
{
    public int ID { get; set; }
    
    // The sleep session we use the wake(end) time from
    public DateOnly PriorSleepID { get; set; } = priorSleep.Date;
    public DailySleep PriorSleep { get; set; } = priorSleep;
    
    // The following sleep session we use the sleep start time from
    public DateOnly SubsequentSleepID { get; set; } = subsequentSleep.Date;
    public DailySleep SubsequentSleep { get; set; } = subsequentSleep;
    
    // Start and end times for the AwakeWindow, thus using the end time 
    public DateTimeOffset StartLocal => PriorSleep.EndLocal;
    public DateTimeOffset EndLocal => SubsequentSleep.StartLocal;
    
    public TimeSpan Duration => EndLocal - StartLocal;
}
