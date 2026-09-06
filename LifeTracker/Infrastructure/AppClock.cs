using LifeTracker.Configuration;
using Microsoft.Extensions.Options;

namespace LifeTracker.Infrastructure;

public sealed class AppClock
{
    private readonly TimeProvider _time;
    private readonly TimeZoneInfo _zone;

    public AppClock(TimeProvider time, IOptions<TimeOptions> options)
    {
        var zoneID = string.IsNullOrWhiteSpace(options.Value.ZoneID) ? TimeOptions.DefaultZoneID : options.Value.ZoneID;
        _time = time;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(zoneID);
    }

    public DateTimeOffset UtcNow => _time.GetUtcNow();

    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _zone).DateTime);

    public DateOnly ToLocalDate(DateTimeOffset utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, _zone).DateTime);

    public DateTimeOffset StartOfLocalDay(DateOnly day) =>
        new(day.ToDateTime(TimeOnly.MinValue), _zone.GetUtcOffset(day.ToDateTime(TimeOnly.MinValue)));
}
