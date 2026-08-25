using System.Text.Json.Serialization;

namespace LifeTracker.Entities.Garmin;

public class HeartRateSample : GarminEntity
{
    [JsonIgnore] // prevents serialization from jumping back up to the parent entity
    public DailyHeartRate DailyHeartRate { get; set; } = null!; // navigation property.  TODO check, test possible json serialiser recursion/circular references cause of this
    public DateTimeOffset Timestamp { get; set; } // composite PK together with Date
    public int BPM { get; set; }
    public bool Sleeping { get; set; } = false;
}
