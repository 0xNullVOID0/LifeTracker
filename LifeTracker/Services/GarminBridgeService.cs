using System.Text.Json;
using LifeTracker.Dtos.Garmin;
using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;

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

        private async Task<T?> GetFromBridgeAsync<T>(string endpoint, DateOnly date)
        {
            var url = $"{endpoint}?date={date:yyyy-MM-dd}";
            using var response = await _httpClient.GetAsync(url); // fetch request with data(could be empty) from Python Garmin Bridge API

            if (response.StatusCode is System.Net.HttpStatusCode.NoContent   // 204 empty
                                    or System.Net.HttpStatusCode.NotFound)   // 404 future
                return default;

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Python Garmin Bridge error {(int)response.StatusCode} for {url}");

            return await response.Content.ReadFromJsonAsync<T>(); // deserialize the response's body to type T
        }

        // TODO rename all to sync?
        // Syncs all Garmin data from the official API via the python GarminConnect bridge to the DB
        public async Task<IResult> SyncAllDataByDay(DateOnly date)
        {
            var heart = await FetchHeartRateByDay(date);
            var stress = await FetchStressLevelByDay(date);
            var sleep = await FetchSleepByDay(date);

            return Results.Ok(new { heart, stress, sleep });
        }

        public async Task<DailyHeartRate?> FetchHeartRateByDay(DateOnly date)
        {
            var heartDto = await GetFromBridgeAsync<DailyHeartRateDto>("heartrate", date);
            if (heartDto is null)
                return null;

            var dailyHeart = MapToEntity(heartDto);
            await SaveDailyHeartRate(dailyHeart);
            return dailyHeart;
        }

        public async Task<DailyStress?> FetchStressLevelByDay(DateOnly date)
        {
            var stressDto = await GetFromBridgeAsync<DailyStressDto>("stress", date);
            if (stressDto is null)
                return null;

            var dailyStress = MapToEntity(stressDto);
            await SaveDailyStress(dailyStress);
            return dailyStress;
        }



        // Upserts DailyHeartRate with it's related HeartRateSamples
        public async Task SaveDailyHeartRate(DailyHeartRate dailyHeart)
        {
            if (dailyHeart == null)
                return;

            try
            {
                // Check if a record already exists for this date including possible child HeartRateSamples
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


        public async Task SaveDailyStress(DailyStress dailyStress)
        {
            if (dailyStress == null)
                return;

            try
            {
                var existing = await _context.DailyStress.FirstOrDefaultAsync(d => d.Date == dailyStress.Date);

                // Update record if already exists
                if (existing != null)
                {
                    existing.Average = dailyStress.Average;
                    existing.Max = dailyStress.Max;
                    // TODO add updatedAt
                }
                else
                {
                    _context.DailyStress.Add(dailyStress);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,"Database error occurred while trying to save/update DailyStress for date {Date}.", dailyStress.Date);
                throw;
            }
        }

        private static DailyHeartRate MapToEntity(DailyHeartRateDto dto) => new()
        {
            Date = dto.CalendarDate,
            RestingRate = dto.RestingHeartRate,
            Min = dto.MinHeartRate, 
            Max = dto.MaxHeartRate,

            // Convert the raw timestamp/BPM array pairs into HeartRateSample entities with a relation to the DailyHeartRate by date as FK
            Samples = dto.HeartRateValues?.Where(v => v.Length >= 2 && v[0].HasValue && v[1].HasValue)
                .Select(v => new HeartRateSample
                {
                    Date = dto.CalendarDate, // foreign Key
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(v[0]!.Value),
                    Bpm = (int)v[1]!.Value
                }).ToList() ?? new List<HeartRateSample>()
        };

        private static DailyStress MapToEntity(DailyStressDto dto) => new()
        {
            Date = dto.CalendarDate,
            Average = dto.AvgStressLevel,
            Max = dto.MaxStressLevel,
        };
    }
    }

