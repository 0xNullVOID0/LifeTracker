using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LifeTracker.Entities.Buienradar;

public class BuienradarStationMeasurement : ClimateMeasurement
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } // composite PK together with stationID

    [JsonPropertyName("stationid")]
    [DefaultValue(6278)]
    public int StationID { get; set; }

    [JsonPropertyName("stationname")]
    [DefaultValue("Meetstation Heino")]
    public string StationName { get; set; } = null!; // null! to calm compiler, stationname is always present

    // multiple can be missing from multiple or many stations, so basically all nullable 
    [JsonPropertyName("weatherdescription")]
    [DefaultValue("Zwaar bewolkt")]
    public string? WeatherDescription { get; set; }

    [JsonPropertyName("winddirection")]
    [DefaultValue("ZZW")] // TODO enum
    public string? WindDirection { get; set; }

    [JsonPropertyName("precipitation")]
    [DefaultValue(0.0f)]
    public float? Precipitation { get; set; }

    [JsonPropertyName("sunpower")]
    [DefaultValue(602.0f)]
    public float? SunPower { get; set; } // TODO is actually an int but buienradar returns 306.0, 680.0 

    [JsonPropertyName("rainFallLastHour")]
    [DefaultValue(0.0f)]
    public float? RainFallLastHour { get; set; } 

    [JsonPropertyName("rainFallLast24Hour")]
    [DefaultValue(3.7f)]
    public float? RainFallLast24Hour { get; set; }

    [JsonPropertyName("windspeed")]
    [DefaultValue(3.0f)]
    public float? Windspeed { get; set; }

    [JsonPropertyName("airpressure")]
    [DefaultValue(1017.2f)]
    public float? AirPressure { get; set; } // almost half of stations don't have it, so nullable


}
