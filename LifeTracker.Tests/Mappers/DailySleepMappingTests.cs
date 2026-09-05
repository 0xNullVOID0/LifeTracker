using LifeTracker.DTOs.Garmin;
using static LifeTracker.Mappers.GarminMapping;

namespace LifeTracker.Tests.Mappers;

public class DailySleepMappingTests
{
    [Fact]
    public void MapToEntity_CopiesSleepSummaryFields()
    {
        var dto = new SleepResponseDTO
        {
            DailySleep = new DailySleepDTO
            {
                CalendarDate = new DateOnly(2026, 8, 18),
                SleepTimeSeconds = 22920,
                DeepSleepSeconds = 7020,
                LightSleepSeconds = 12480,
                RemSleepSeconds = 3420,
                AwakeSleepSeconds = 3300,
                AvgHeartRate = 56.0,
                AvgSleepStress = 11.0
            }
        };

        var entity = MapToEntity(dto);

        Assert.Equal(new DateOnly(2026, 8, 18), entity.Date);
        Assert.Equal(22920, entity.SleepTimeSeconds);
        Assert.Equal(7020, entity.DeepSleepSeconds);
        Assert.Equal(12480, entity.LightSleepSeconds);
        Assert.Equal(3420, entity.RemSleepSeconds);
        Assert.Equal(3300, entity.AwakeSleepSeconds);
        Assert.Equal(56, entity.AvgHeartRate);
        Assert.Equal(11, entity.AvgSleepStress);
    }
}
