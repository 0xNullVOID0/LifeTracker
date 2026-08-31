using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LifeTracker.Entities.ESP32;

public class BuienradarStationMeasurement : ClimateMeasurement
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } // composite PK together with stationID

    [JsonPropertyName("stationid")]
    public int StationID { get; set; }

    [JsonPropertyName("stationname")]
    public string StationName { get; set; } = null!; // null! to calm compiler, stationname is always present

    // multiple can be missing from multiple or many stations, so basically all nullable 
    [JsonPropertyName("weatherdescription")]
    public string? WeatherDescription { get; set; }

    [JsonPropertyName("winddirection")]
    public string? WindDirection { get; set; }

    [JsonPropertyName("precipitation")]
    public float? Precipitation { get; set; }

    [JsonPropertyName("sunpower")]
    public float? SunPower { get; set; }

    [JsonPropertyName("rainFallLastHour")]
    public float? RainFallLastHour { get; set; } 

    [JsonPropertyName("rainFallLast24Hour")]
    public float? RainFallLast24Hour { get; set; }

    [JsonPropertyName("windspeedBft")]
    public float? WindspeedBft { get; set; }

    [JsonPropertyName("airpressure")]
    public float? AirPressure { get; set; } // almost half of stations don't have it, so nullable


}
