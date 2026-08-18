using LifeTracker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LifeTracker.Services
{
    public class GarminBridgeService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<GarminBridgeService> _logger;

        public GarminBridgeService(HttpClient httpclient, AppDbContext context, ILogger<GarminBridgeService> logger)
        {
            _httpClient = httpclient;
            _context = context;
            _logger = logger;
        }

        public async Task<DailyHeartRate> FetchHeartRateByDay(DateOnly date)
        {
            // map date to correct format for api
            string url = $"heartrate/{date:yyyy-MM-dd}";

            var heart_dto = await _httpClient.GetFromJsonAsync<DailyHeartRateDto>(url);

            if (heart_dto == null)
                return null;

            // map DTO to database entity and return it
            return MapToEntity(heart_dto);
        }

        public async Task<DailyStress> FetchStressLevelByDay(DateOnly date)
        {
            // map date to correct format for api
            string url = $"stress/{date:yyyy-MM-dd}";

            var stress_dto = await _httpClient.GetFromJsonAsync<DailyStressDto>(url);

            if (stress_dto == null)
                return null;

            // map DTO to database entity and return it
            return MapToEntity(stress_dto);
        }

        private static DailyHeartRate MapToEntity(DailyHeartRateDto dto) => new()
        {
            Date = dto.CalendarDate,
            Values = dto.HeartRateValues,
            RestingRate = dto.RestingHeartRate,
            Min = dto.MinHeartRate, 
            Max = dto.MaxHeartRate,
        };

        private static DailyStress MapToEntity(DailyStressDto dto) => new()
        {
            Date = dto.CalendarDate,
            Values = dto.StressValuesArray,
            Average = dto.AvgStressLevel,
            Max = dto.MaxStressLevel,
        };
    }


    public class DailyHeartRate
    {
        public int ID { get; set; }

        public DateOnly Date { get; set; }
        public List<long?[]>? Values { get; set; }

        public int? RestingRate { get; set; }

        public int? Min { get; set; }

        public int? Max { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DailyHeartRateDto
    {
        [JsonPropertyName("calendarDate")]
        public DateOnly CalendarDate { get; set; }

        [JsonPropertyName("startTimestampGMT")]
        public DateTime StartTimestampGmt { get; set; }

        [JsonPropertyName("endTimestampGMT")]
        public DateTime EndTimestampGmt { get; set; }

        [JsonPropertyName("startTimestampLocal")]
        public DateTime StartTimestampLocal { get; set; }

        [JsonPropertyName("endTimestampLocal")]
        public DateTime EndTimestampLocal { get; set; }

        [JsonPropertyName("minHeartRate")]
        public int? MinHeartRate { get; set; }

        [JsonPropertyName("maxHeartRate")]
        public int? MaxHeartRate { get; set; }

        [JsonPropertyName("restingHeartRate")]
        public int? RestingHeartRate { get; set; }

        [JsonPropertyName("lastSevenDaysAvgRestingHeartRate")]
        public int? SevenDaysAvgRestingHeartRate { get; set; }

        [JsonPropertyName("heartRateValues")]
        public List<long?[]>? HeartRateValues { get; set; }
    }

    // TOOD rename to DailyStressLevel?
    public class DailyStress
    {
        public int ID { get; set; }

        public DateOnly Date { get; set; }
        public List<long?[]>? Values { get; set; }

        public int? Average { get; set; }

        public int? Max { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DailyStressDto
    {
        [JsonPropertyName("calendarDate")]
        public DateOnly CalendarDate { get; set; }

        [JsonPropertyName("startTimestampGMT")]
        public DateTime StartTimestampGmt { get; set; }

        [JsonPropertyName("endTimestampGMT")]
        public DateTime EndTimestampGmt { get; set; }

        [JsonPropertyName("startTimestampLocal")]
        public DateTime StartTimestampLocal { get; set; }

        [JsonPropertyName("endTimestampLocal")]
        public DateTime EndTimestampLocal { get; set; }

        [JsonPropertyName("maxStressLevel")]
        public int? MaxStressLevel { get; set; }

        [JsonPropertyName("avgStressLevel")]
        public int? AvgStressLevel { get; set; }

        [JsonPropertyName("stressChartValueOffset")]
        public int? StressChartValueOffset { get; set; }

        [JsonPropertyName("stressChartYAxisOrigin")]
        public int? StressChartYAxisOrigin { get; set; }

        [JsonPropertyName("stressValuesArray")]
        public List<long?[]>? StressValuesArray { get; set; }

        // uses JsonElement[] because the array contains mixed types (long timestamps, string "MEASURED", ints)
        [JsonPropertyName("bodyBatteryValuesArray")]
        public List<JsonElement[]>? BodyBatteryValuesArray { get; set; }
    }
}
