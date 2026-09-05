using System.Text.Json.Serialization;

namespace LifeTracker.DTOs.ActivityWatch;

public sealed record ActivityEventDTO
{
    [JsonPropertyName("id")] public int ID { get; set; }

    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }

    [JsonPropertyName("duration")] public double Duration { get; set; }

    [JsonPropertyName("data")] public EventDataDTO Data { get; set; } = new();
}
