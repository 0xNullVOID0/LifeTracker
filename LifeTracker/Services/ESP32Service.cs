using System.Globalization;
using System.Net;
using LifeTracker.Dtos.Garmin;
using LifeTracker.Entities.ESP32;
using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;


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

    public async Task<RoomClimateMeasurement> SaveRoomClimate(RoomClimateMeasurement roomClimate)
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
