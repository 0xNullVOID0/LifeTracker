using LifeTracker.Entities.ESP32;
using LifeTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker.Services;

public class ESP32Service
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly AppClock _clock;
    private readonly ILogger<ESP32Service> _logger;

    public ESP32Service(HttpClient httpClient, AppDbContext context, AppClock clock, ILogger<ESP32Service> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _clock = clock;
        _logger = logger;
    }

    // TODO decide on truncating response, averaging the 15 second samples to 1 minute since drawing the charts on frontend still takes a bit even with just waiting on 1 days data now, still thousands of points to draw and calculate
    public async Task<List<RoomClimateMeasurement>?> GetByDay(DateOnly date)
    {
        var start = _clock.StartOfLocalDay(date);
        var end = _clock.StartOfLocalDay(date.AddDays(1));

        var data = await _context.RoomClimateMeasurements.AsNoTracking()
            .Where(m => m.Timestamp >= start && m.Timestamp < end)
            .OrderBy(m => m.Timestamp).ToListAsync();

        return data.Count > 0 ? data : null;
    }

    public async Task<List<RoomClimateMeasurement>?> GetRange(DateTimeOffset start, DateTimeOffset end)
    {
        var data = await _context.RoomClimateMeasurements.AsNoTracking()
            .Where(m => m.Timestamp >= start && m.Timestamp <= end)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return data.Count > 0 ? data : null;
    }

    public async Task<RoomClimateMeasurement?> SaveRoomClimate(RoomClimateMeasurement? roomClimate)
    {
        if (roomClimate is null)
            return null;

        try
        {
            _context.RoomClimateMeasurements.Add(roomClimate);
            await _context.SaveChangesAsync();
            return roomClimate;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while trying to save RoomClimateMeasurement");
            throw;
        }
    }
}
