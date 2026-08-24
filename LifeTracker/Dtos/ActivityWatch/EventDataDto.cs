using System.Text.Json.Serialization;

namespace LifeTracker.Dtos.ActivityWatch;

public class EventDataDto
{
    [JsonPropertyName("app")]
    public string App { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
