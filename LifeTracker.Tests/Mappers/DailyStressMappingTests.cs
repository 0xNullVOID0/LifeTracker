using LifeTracker.DTOs.Garmin;
using static LifeTracker.Mappers.GarminMapping;


namespace LifeTracker.Tests.Mappers;

public class DailyStressMappingTests
{
    [Fact]
    public void MapToEntity_CopiesDateAverageAndMax()
    {
        var dto = new DailyStressDTO
        {
            CalendarDate = new DateOnly(2026, 8, 18), AvgStressLevel = 17, MaxStressLevel = 87
        };

        var entity = MapToEntity(dto);

        Assert.Equal(dto.CalendarDate, entity.Date);
        Assert.Equal(17, entity.Average);
        Assert.Equal(87, entity.Max);
    }
}
