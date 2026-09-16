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


    // Npgsql timestamptz only accepts DateTimeOffset with offset 0.
    public DateTimeOffset StartOfLocalDay(DateOnly day)
    {
        var localMidnight = DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, _zone), TimeSpan.Zero);
    }
}
