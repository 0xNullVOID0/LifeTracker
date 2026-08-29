using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LifeTracker.Entities.ESP32;

public class BuienradarStationMeasurement : ClimateMeasurement
{
    public int ID { get; set; }

    [JsonPropertyName("stationid")]
    public int StationId { get; set; }

    [JsonPropertyName("stationname")]
    public string StationName { get; set; }

    [JsonPropertyName("windspeedBft")]
    public float? WindspeedBft { get; set; }

    [JsonPropertyName("airpressure")]
    public float AirPressure { get; set; }
}
