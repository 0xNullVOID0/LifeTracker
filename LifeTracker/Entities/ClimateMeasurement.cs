using System.Text.Json.Serialization;

namespace LifeTracker.Entities;

public abstract class ClimateMeasurement : BaseEntity
{
    // TODO add ID
    [JsonPropertyName("temperature")]

    public float Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public float Humidity { get; set; }
}
