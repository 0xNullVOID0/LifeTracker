using System.Text.Json.Serialization;

namespace LifeTracker.Dtos.Garmin;

// TOOD rename to DailyStressLevel?
public class DailyHeartRateDto
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

    [JsonPropertyName("minHeartRate")]
    public int MinHeartRate { get; set; }

    [JsonPropertyName("maxHeartRate")]
    public int MaxHeartRate { get; set; }

    [JsonPropertyName("restingHeartRate")]
    public int RestingHeartRate { get; set; }

    [JsonPropertyName("lastSevenDaysAvgRestingHeartRate")]
    public int? SevenDaysAvgRestingHeartRate { get; set; }

    [JsonPropertyName("heartRateValues")]
    public List<long?[]>? HeartRateValues { get; set; }
}
