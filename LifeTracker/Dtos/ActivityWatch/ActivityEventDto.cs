using System.Text.Json.Serialization;

namespace LifeTracker.Dtos.ActivityWatch;

public class ActivityEventDto
{
    [JsonPropertyName("id")]
    public int ID { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("data")]
    public EventDataDto Data { get; set; } = new();
}
