using System.Text.Json.Serialization;

namespace LifeTracker.DTOs.ActivityWatch;

public sealed record EventDataDTO
{
    [JsonPropertyName("app")] public string App { get; set; } = string.Empty;

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
}
