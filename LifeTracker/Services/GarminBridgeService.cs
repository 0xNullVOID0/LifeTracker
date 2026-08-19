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

        

        public async Task<object?> GarminBridgeHealthCheck() =>
            await _httpClient.GetFromJsonAsync<object?>("health");

        public async Task<DailyHeartRate> FetchHeartRateByDay(DateOnly date)
        {
            // map date to correct format for api
            string url = $"heartrate/{date:yyyy-MM-dd}";

            var heart_dto = await _httpClient.GetFromJsonAsync<DailyHeartRateDto>(url);

            if (heart_dto == null)
                return null;

            // map DTO to domain entity, save and return
            var dailyHeart = MapToEntity(heart_dto);
            await SaveDailyHeartRate(dailyHeart);

            return dailyHeart;
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

        // Upserts DailyHeartRate with it's related HeartRateSamples
        public async Task SaveDailyHeartRate(DailyHeartRate dailyHeart)
        {
            if (dailyHeart == null)
                return;

            try
            {
                // Check if a record already exists for this date, including child samples
                var existing = await _context.DailyHeartRate
                    .Include(d => d.Samples)
                    .FirstOrDefaultAsync(d => d.Date == dailyHeart.Date);

                if (existing != null)
                {
                    // Update summary properties
                    existing.RestingRate = dailyHeart.RestingRate;
                    existing.Min = dailyHeart.Min;
                    existing.Max = dailyHeart.Max;

                    // Delete old samples from the context to prevent orphaned records/foreign key conflicts
                    _context.HeartRateSample.RemoveRange(existing.Samples);

                    // Attach the new sample list
                    existing.Samples = dailyHeart.Samples;
                }
                else
                {
                    // Insert new entity along with its samples
                    _context.DailyHeartRate.Add(dailyHeart);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while trying to save/update DailyHeartRate for date {Date}.", dailyHeart.Date);
                throw;
            }
        }

        private static DailyHeartRate MapToEntity(DailyHeartRateDto dto) => new()
        {
            Date = dto.CalendarDate,
            Values = dto.HeartRateValues,
            RestingRate = dto.RestingHeartRate,
            Min = dto.MinHeartRate, 
            Max = dto.MaxHeartRate,

            // Convert the raw timestamp/BPM array pairs into HeartRateSample entities with a relation to the DailyHeartRate by date as FK
            Samples = dto.HeartRateValues?.Where(v => v.Length >= 2 && v[0].HasValue && v[1].HasValue)
                .Select(v => new HeartRateSample
                {
                    DailyHeartRateDate = dto.CalendarDate, // foreign Key
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(v[0]!.Value),
                    Bpm = (int)v[1]!.Value
                }).ToList() ?? new List<HeartRateSample>()
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
        public DateOnly Date { get; set; } // primary key
        public int? RestingRate { get; set; }

        public int? Min { get; set; }

        public int? Max { get; set; }
        public List<HeartRateSample> Samples { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class HeartRateSample
    {
        public DateOnly DailyHeartRateDate { get; set; } // foreign key

        [JsonIgnore] // prevents serialization from jumping back up to the parent entity
        public DailyHeartRate DailyHeartRate { get; set; } = null!; // navigation property.  TODO check, test possible json serialiser recursion/circular references cause of this

        public DateTimeOffset Timestamp { get; set; }
        public int Bpm { get; set; }
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
        public DateOnly Date { get; set; } // primary key
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
