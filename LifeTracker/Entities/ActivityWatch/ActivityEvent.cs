namespace LifeTracker.Entities.ActivityWatch;

public class ActivityEvent
{
    public int ID { get; set; }
    public int AwID { get; set; }
    public DateTime Timestamp { get; set; }
    public double Duration { get; set; }
    public string App { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
