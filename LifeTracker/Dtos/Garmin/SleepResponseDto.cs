using System.Text.Json.Serialization;

namespace LifeTracker.Dtos.Garmin;

public class SleepResponseDto
{
    [JsonPropertyName("dailySleepDTO")]
    public DailySleepDto DailySleep { get; set; } = new();

    [JsonPropertyName("sleepHeartRate")]
    public List<GarminTimeSampleDto>? SleepHeartRate { get; set; }
}

public class DailySleepDto
{
    [JsonPropertyName("calendarDate")]
    public DateOnly CalendarDate { get; set; }

    [JsonPropertyName("sleepTimeSeconds")]
    public int SleepTimeSeconds { get; set; }

    [JsonPropertyName("deepSleepSeconds")]
    public int DeepSleepSeconds { get; set; }

    [JsonPropertyName("lightSleepSeconds")]
    public int LightSleepSeconds { get; set; }

    [JsonPropertyName("remSleepSeconds")]
    public int RemSleepSeconds { get; set; }

    [JsonPropertyName("awakeSleepSeconds")]
    public int AwakeSleepSeconds { get; set; }

    [JsonPropertyName("avgHeartRate")]
    public double AvgHeartRate { get; set; }

    [JsonPropertyName("avgSleepStress")]
    public double AvgSleepStress { get; set; }
}

public class GarminTimeSampleDto
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("startGMT")]
    public long StartGmt { get; set; }
}
