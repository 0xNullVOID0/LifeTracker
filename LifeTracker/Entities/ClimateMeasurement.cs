using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LifeTracker.Entities;

public abstract class ClimateMeasurement : BaseEntity
{
    // TODO add ID
    [JsonPropertyName("temperature")]
    [DefaultValue(19.1f)]
    public float Temperature { get; set; }

    [JsonPropertyName("humidity")]
    [DefaultValue(71.0f)]
    public float Humidity { get; set; } // TODO is actually an int but buienradar returns 71.0
}
