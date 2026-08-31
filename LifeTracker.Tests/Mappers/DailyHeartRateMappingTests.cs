using LifeTracker.Dtos.Garmin;
using LifeTracker.Services;

namespace LifeTracker.Tests.Mappers;

public class DailyHeartRateMappingTests
{
    [Fact]
    public void MapToEntity_CopiesSummaryAndValidSamples()
    {
        var date = new DateOnly(2026, 8, 18);
        var dto = new DailyHeartRateDto
        {
            CalendarDate = date,
            RestingHeartRate = 55,
            MinHeartRate = 49,
            MaxHeartRate = 138,
            HeartRateValues =
            [
                [1787063520000, 62],
                [1787063640000, 64],

                // these should be skipped
                [1787063760000, null],  
                [null, 70]               
            ]
        };

        var entity = GarminBridgeService.MapToEntity(dto);

        Assert.Equal(date, entity.Date);
        Assert.Equal(55, entity.RestingRate);
        Assert.Equal(49, entity.Min);
        Assert.Equal(138, entity.Max);
        Assert.Equal(2, entity.Samples.Count); // should only be 2 instead of 4
        Assert.All(entity.Samples, s => Assert.Equal(date, s.Date)); // check if associated HeartRateSamples are set properly 
        Assert.Equal(62, entity.Samples[0].BPM);
        Assert.Equal(64, entity.Samples[1].BPM);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1787063520000), entity.Samples[0].Timestamp);
    }
}
