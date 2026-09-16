using LifeTracker.Entities.ESP32;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker.Services;

public class ESP32Service
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<ESP32Service> _logger;

    public ESP32Service(HttpClient httpClient, AppDbContext context, ILogger<ESP32Service> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoomClimateMeasurement>?> GetByDay(DateOnly date)
    {
        var start = new DateTimeOffset(date, TimeOnly.MinValue, TimeSpan.Zero);
        var end = new DateTimeOffset(date.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        var data = await _context.RoomClimateMeasurements.AsNoTracking()
            .Where(m => m.Timestamp >= start && m.Timestamp < end)
            .OrderBy(m => m.Timestamp).ToListAsync();

        return AverageToMinute(data);
    }

    public async Task<List<RoomClimateMeasurement>?> GetRange(DateTimeOffset start, DateTimeOffset end)
    {
        var data = await _context.RoomClimateMeasurements.AsNoTracking()
            .Where(m => m.Timestamp >= start && m.Timestamp <= end)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return AverageToMinute(data);
    }

    // Average the 15s ingest to 1 minute samples to not overload the charts on frontend and improve performance. 
    private static List<RoomClimateMeasurement>? AverageToMinute(List<RoomClimateMeasurement> data)
    {
        if (data.Count == 0)
            return null;

        return data.GroupBy(m => new DateTimeOffset(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, m.Timestamp.Minute, 0, m.Timestamp.Offset))
            .OrderBy(g => g.Key)
            .Select(g => new RoomClimateMeasurement
            {
                Timestamp = g.Key,
                CO2 = (int)Math.Round(g.Average(x => x.CO2)),
                Temperature = g.Average(x => x.Temperature),
                Humidity = g.Average(x => x.Humidity)
            })
            .ToList();
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
