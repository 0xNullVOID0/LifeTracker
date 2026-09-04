using LifeTracker.Dtos.Buienradar;
using LifeTracker.Entities.Buienradar;
using Microsoft.EntityFrameworkCore;


namespace LifeTracker.Services;

public class BuienradarService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public BuienradarService(HttpClient httpclient, AppDbContext context)
    {
        _httpClient = httpclient;
        _context = context;
    }

    public async Task<List<BuienradarStationMeasurement>> GetAll() =>
        await _context.BuienradarStationMeasurements.AsNoTracking().ToListAsync();

    // TODO add station id/name as parameter
    public async Task<BuienradarStationMeasurement?> SyncStationMeasurement()
    {
        // Get JSON weather data from buienradar
        var data = await _httpClient.GetFromJsonAsync<BuienradarResponse>(_httpClient.BaseAddress);

        // Get closest station
        var station = data?.Actual?.StationMeasurements?
            .FirstOrDefault(s => s.StationName.Contains("Heino") || s.StationID == 6278); // TODO make configurable

        if (station != null) 
            await SaveMeasurement(station);
        return station;
    }

    public async Task SaveMeasurement(BuienradarStationMeasurement measurement)
    {
        // Convert timestamp to UTC for saving to DB and checking for existing record
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        DateTime rawDateTime = measurement.Timestamp.DateTime;
        DateTime unspecified = DateTime.SpecifyKind(rawDateTime, DateTimeKind.Unspecified);
        measurement.Timestamp = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone));

        // Check if record with composite PK already exists or not before trying to save
        var existing = await _context.BuienradarStationMeasurements.FindAsync(measurement.StationID, measurement.Timestamp);

        if (existing is null)
        {
            _context.BuienradarStationMeasurements.Add(measurement);
            await _context.SaveChangesAsync();
        }
    }
}

