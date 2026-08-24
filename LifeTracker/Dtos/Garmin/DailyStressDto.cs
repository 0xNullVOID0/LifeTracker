using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeTracker.Dtos.Garmin;

// TOOD rename to DailyStressLevel?
public class DailyStressDto
{
    [JsonPropertyName("calendarDate")]
    public DateOnly CalendarDate { get; set; }

    [JsonPropertyName("startTimestampGMT")]
    public DateTime StartTimestampGmt { get; set; }

    [JsonPropertyName("endTimestampGMT")]
    public DateTime EndTimestampGmt { get; set; }

    [JsonPropertyName("startTimestampLocal")]
    public DateTime StartTimestampLocal { get; set; }

    [JsonPropertyName("endTimestampLocal")]
    public DateTime EndTimestampLocal { get; set; }

    [JsonPropertyName("maxStressLevel")]
    public int? MaxStressLevel { get; set; }

    [JsonPropertyName("avgStressLevel")]
    public int? AvgStressLevel { get; set; }

    [JsonPropertyName("stressChartValueOffset")]
    public int? StressChartValueOffset { get; set; }

    [JsonPropertyName("stressChartYAxisOrigin")]
    public int? StressChartYAxisOrigin { get; set; }

    [JsonPropertyName("stressValuesArray")]
    public List<long?[]>? StressValuesArray { get; set; }

    // uses JsonElement[] because the array contains mixed types (long timestamps, string "MEASURED", ints)
    [JsonPropertyName("bodyBatteryValuesArray")]
    public List<JsonElement[]>? BodyBatteryValuesArray { get; set; }
}
