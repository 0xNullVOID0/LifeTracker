using System.Text.Json.Serialization;
using LifeTracker.Dtos.Buienradar;
using LifeTracker.Entities;
using LifeTracker.Entities.ESP32;


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


    public async Task<BuienradarStationMeasurement?> GetBuienradarDataAsync()
    {
        // Get JSON weather data from buienradar
        var data = await _httpClient.GetFromJsonAsync<BuienradarResponse>(_httpClient.BaseAddress);

        // Get closest station
        var station = data?.Actual?.StationMeasurements
            .FirstOrDefault(s => s.StationName.Contains("Heino") || s.StationId == 6278); // TODO make configurable

        await SaveMeasurementAsync(station);
        return station;
    }

    public async Task SaveMeasurementAsync(BuienradarStationMeasurement measurement)
    {
        _context.BuienradarStationMeasurements.Add(measurement);
        await _context.SaveChangesAsync();
    }
}

