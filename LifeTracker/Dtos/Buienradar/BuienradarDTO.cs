using System.Text.Json.Serialization;
using LifeTracker.Entities.ESP32;

namespace LifeTracker.Dtos.Buienradar;

public class BuienradarResponse
{
    [JsonPropertyName("actual")]
    public ActualData Actual { get; set; }
}

public class ActualData
{
    [JsonPropertyName("actualradarurl")]
    public string? ActualRadarUrl { get; set; }

    [JsonPropertyName("sunrise")]
    public DateTime Sunrise { get; set; }

    [JsonPropertyName("sunset")]
    public DateTime Sunset { get; set; }

    [JsonPropertyName("stationmeasurements")]
    public List<BuienradarStationMeasurement> StationMeasurements { get; set; } = [];
}


