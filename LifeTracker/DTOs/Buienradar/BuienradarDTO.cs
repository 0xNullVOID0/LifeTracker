using System.Text.Json.Serialization;
using LifeTracker.Entities.Buienradar;

namespace LifeTracker.DTOs.Buienradar;

public sealed record BuienradarResponse
{
    [JsonPropertyName("actual")] public ActualData Actual { get; set; } = new();
}

public sealed record ActualData
{
    [JsonPropertyName("actualradarurl")] public string? ActualRadarUrl { get; set; }

    [JsonPropertyName("sunrise")] public DateTime Sunrise { get; set; }

    [JsonPropertyName("sunset")] public DateTime Sunset { get; set; }

    [JsonPropertyName("stationmeasurements")]
    public List<BuienradarStationMeasurement> StationMeasurements { get; set; } = [];
}
