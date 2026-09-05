using LifeTracker.DTOs.Garmin;
using LifeTracker.Entities.Garmin;

namespace LifeTracker.Mappers;

public static class GarminMapping
{
    internal static DailyHeartRate MapToEntity(DailyHeartRateDTO dto) => new()
    {
        Date = dto.CalendarDate,
        RestingRate = dto.RestingHeartRate,
        Min = dto.MinHeartRate,
        Max = dto.MaxHeartRate,

        // Convert the raw timestamp/BPM array pairs into HeartRateSample entities with a relation to the DailyHeartRate by date as FK
        Samples = dto.HeartRateValues?.Where(v => v.Length >= 2 && v[0].HasValue && v[1].HasValue)
            .Select(v => new HeartRateSample
            {
                Date = dto.CalendarDate, // foreign Key
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(v[0]!.Value), // TODO fix timezones
                BPM = (int)v[1]!.Value
            }).ToList() ?? new List<HeartRateSample>()
    };

    internal static DailyStress MapToEntity(DailyStressDTO dto) => new()
    {
        Date = dto.CalendarDate, Average = dto.AvgStressLevel, Max = dto.MaxStressLevel,
    };

    internal static DailySleep MapToEntity(SleepResponseDTO dto)
    {
        var d = dto.DailySleep;
        var date = d.CalendarDate;

        return new DailySleep
        {
            Date = date,
            SleepTimeSeconds = d.SleepTimeSeconds,
            DeepSleepSeconds = d.DeepSleepSeconds,
            LightSleepSeconds = d.LightSleepSeconds,
            RemSleepSeconds = d.RemSleepSeconds,
            AwakeSleepSeconds = d.AwakeSleepSeconds,
            AvgHeartRate = (int)d.AvgHeartRate,
            AvgSleepStress = (int)d.AvgSleepStress,
        };
    }
}
