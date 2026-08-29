using System.Text.Json.Serialization;
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
        var station = data?.Actual?.BuienradarStationMeasurements
            .FirstOrDefault(s => s.StationName.Contains("Heino") || s.StationId == 6278); // TODO make configurable

        // basic debug console print
        if (station is not null)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm}] Station {station.StationName}:");
            Console.WriteLine($"Temp: {station.Temperature}°C");
            Console.WriteLine($"Luchtvochtigheid: {station.Humidity}%");
            Console.WriteLine($"Wind: {station.WindspeedBft} Bft");
            Console.WriteLine($"Luchtdruk: {station.AirPressure} hPa");
        }

        // basic temp test save
        await SaveMeasurementAsync(station);

        return station;
    }

    public async Task SaveMeasurementAsync(BuienradarStationMeasurement measurement)
    {
        _context.BuienradarStationMeasurements.Add(measurement);
        await _context.SaveChangesAsync();
    }
}

public class BuienradarResponse
{
    [JsonPropertyName("actual")]
    public ActualData Actual { get; set; }
}

public class ActualData
{
    [JsonPropertyName("stationmeasurements")]
    public List<BuienradarStationMeasurement> BuienradarStationMeasurements { get; set; }
}


