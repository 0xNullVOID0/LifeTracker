using System.Text.Json.Serialization;
using System.Net.Http.Json;


namespace LifeTracker.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public WeatherService(HttpClient httpclient, AppDbContext context)
        {
            _httpClient = httpclient;
            _context = context;
        }


        public async Task<StationMeasurement?> GetBuienradarDataAsync()
        {
            // Get JSON weather data from buienradar
            var data = await _httpClient.GetFromJsonAsync<BuienradarResponse>(_httpClient.BaseAddress);

            // Get closest station
            var station = data?.Actual?.StationMeasurements
                .FirstOrDefault(s => s.StationName.Contains("Heino") || s.StationId == 6278);

            // basic debug console print
            if (station != null)
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

        public async Task SaveMeasurementAsync(StationMeasurement measurement)
        {
            _context.WeatherLogs.Add(measurement);
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
        public List<StationMeasurement> StationMeasurements { get; set; }
    }

    public class StationMeasurement
    {
        public int ID { get; set; }

        [JsonPropertyName("stationid")]
        public int StationId { get; set; }

        [JsonPropertyName("stationname")]
        public string StationName { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("humidity")]
        public float? Humidity { get; set; }

        [JsonPropertyName("windspeedBft")]
        public float? WindspeedBft { get; set; }

        [JsonPropertyName("airpressure")]
        public float? AirPressure { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}


