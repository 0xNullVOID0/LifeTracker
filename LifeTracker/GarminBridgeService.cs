using LifeTracker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace LifeTracker
{
    public class GarminBridgeService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ILogger<ActivityWatchService> _logger;

        public GarminBridgeService(HttpClient httpclient, AppDbContext context, ILogger<ActivityWatchService> logger)
        {
            _httpClient = httpclient;
            _context = context;
            _logger = logger;
        }

        public async Task<DailyHeartRate> FetchTodaysHeartRate() =>
            await FetchHeartRateByDay(DateOnly.FromDateTime(DateTime.Now));

        public async Task<DailyHeartRate> FetchHeartRateByDay(DateOnly date)
        {
            // map date to correct format for api
            string url = $"heartrate/{date.ToString("O")}";

            var heart_dto = await _httpClient.GetFromJsonAsync<DailyHeartRateDto>(url);

            if (heart_dto == null)
                return null;

            // map DTO to database entity and return it
            return MapToEntity(heart_dto);
        }

        private static DailyHeartRate MapToEntity(DailyHeartRateDto dto) => new()
        {
            Date = dto.CalendarDate,
            Values = dto.HeartRateValues,
            RestingRate = dto.RestingHeartRate,
            Min = dto.MinHeartRate, 
            Max = dto.MaxHeartRate,
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
}
