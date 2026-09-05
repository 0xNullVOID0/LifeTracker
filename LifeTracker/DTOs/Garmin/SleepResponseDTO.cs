using System.Text.Json.Serialization;

namespace LifeTracker.DTOs.Garmin;

public sealed record SleepResponseDTO
{
    [JsonPropertyName("dailySleepDTO")] public DailySleepDTO DailySleep { get; set; } = new();

    [JsonPropertyName("sleepHeartRate")] public List<GarminTimeSampleDTO>? SleepHeartRate { get; set; }
}

public sealed record DailySleepDTO
{
    [JsonPropertyName("calendarDate")] public DateOnly CalendarDate { get; set; }

    [JsonPropertyName("sleepTimeSeconds")] public int SleepTimeSeconds { get; set; }

    [JsonPropertyName("deepSleepSeconds")] public int DeepSleepSeconds { get; set; }

    [JsonPropertyName("lightSleepSeconds")]
    public int LightSleepSeconds { get; set; }

    [JsonPropertyName("remSleepSeconds")] public int RemSleepSeconds { get; set; }

    [JsonPropertyName("awakeSleepSeconds")]
    public int AwakeSleepSeconds { get; set; }

    [JsonPropertyName("avgHeartRate")] public double AvgHeartRate { get; set; }

    [JsonPropertyName("avgSleepStress")] public double AvgSleepStress { get; set; }
}

public sealed record GarminTimeSampleDTO
{
    [JsonPropertyName("value")] public int Value { get; set; }

    [JsonPropertyName("startGMT")] public long StartGmt { get; set; }
}
