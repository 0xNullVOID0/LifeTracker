using System.Text.Json;
using LifeTracker.Dtos.Garmin;
using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker.Services;

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

    // TODO cancellation tokens? since its such a long duration function
    // TODO turn into background task or something
    // Function for syncing larger stretches of Garming data such as for a first time setup
    public async Task<BackfillResult> SyncRecentDays(int days = 14)
    {
        days = Math.Clamp(days, 1, 31); // cap backfill to 1 month to prevent overloading the Garmin API and getting rate limited. TODO add slower background task later for syncing long term profile data
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int synced = 0;
        int empty = 0;

        // Loop from oldest to today for most organic data entry and prevent possible conflicts as best as possible
        for (var i = days - 1; i >= 0; i--) 
        {
            var date = today.AddDays(-i);

            try
            {
                GarminDay day = await SyncAllDataByDay(date);
                if (day is null) empty++;
                else synced++;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Backfill stopped at {Date}", date);
                return new BackfillResult(synced, empty, StoppedAt: date, Error: ex.Message);
            }

            await Task.Delay(400); // delay to prevent hard spamming the official Garmin API and get rate limited
        }

        return new BackfillResult(synced, empty, StoppedAt: null, Error: null);
    }

    public sealed record BackfillResult(int Synced, int Empty, DateOnly? StoppedAt, string? Error);

        // TODO add polly
    private async Task<T?> FetchFromBridgeAsync<T>(string endpoint, DateOnly date)
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

    public async Task<DailyHeartRate?> GetHeartRateByDay(DateOnly date) =>
        await _context.DailyHeartRate.AsNoTracking().Include(d => d.Samples).FirstOrDefaultAsync(d => d.Date == date);

    public async Task<DailyStress?> GetStressByDay(DateOnly date) =>
        await _context.DailyStress.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

        public async Task<DailySleep?> GetSleepByDay(DateOnly date) =>
            await _context.DailySleep.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

        // Gets all Garmin data from DB by date
    public async Task<GarminDay?> GetAllDataByDay(DateOnly date)
        {
        var heart = await GetHeartRateByDay(date);
        var stress = await GetStressByDay(date);
        var sleep = await GetSleepByDay(date);

            if (heart is null && stress is null && sleep is null)
            return null;

        return new GarminDay(date, heart, stress, sleep);
    }

            return Results.Ok(new { heart, stress, sleep });
        }

        // Syncs all Garmin data from the official API via the python GarminConnect bridge and upserts into DB
    public async Task<GarminDay> SyncAllDataByDay(DateOnly date)
        {
            var heart = await SyncHeartRateByDay(date);
            var stress = await SyncStressLevelByDay(date);
            var sleep = await SyncSleepByDay(date);

        if (heart is null && stress is null && sleep is null)
            return null;

        return new GarminDay(date, heart, stress, sleep);
        }

        public async Task<DailyHeartRate?> SyncHeartRateByDay(DateOnly date)
        {
        var heartDTO = await FetchFromBridgeAsync<DailyHeartRateDto>("heartrate", date);
        if (heartDTO is null)
                return null;

        var dailyHeart = MapToEntity(heartDTO);
            await SaveDailyHeartRate(dailyHeart);
            return dailyHeart;
        }

        public async Task<DailyStress?> SyncStressLevelByDay(DateOnly date)
        {
        var stressDTO = await FetchFromBridgeAsync<DailyStressDto>("stress", date);
        if (stressDTO is null)
                return null;

        var dailyStress = MapToEntity(stressDTO);
            await SaveDailyStress(dailyStress);
            return dailyStress;
        }

        // TODO function, cron for getting all backlog data, a first time profile setup to get all available data history, possible rate limit stuff 
        public async Task<DailySleep?> SyncSleepByDay(DateOnly date)
        {
        var sleepDTO = await FetchFromBridgeAsync<SleepResponseDto>("sleep", date);
        if (sleepDTO is null)
                return null;

        var dailySleep = MapToEntity(sleepDTO);
        await SaveDailySleep(dailySleep, sleepDTO.SleepHeartRate);
            return dailySleep;
        }


        // Upserts DailyHeartRate with it's related HeartRateSamples
        public async Task SaveDailyHeartRate(DailyHeartRate dailyHeart)
        {
            if (dailyHeart is null)
                return;

            try
            {
                // Check if a record already exists for this date including possible child HeartRateSamples
                var existing = await _context.DailyHeartRate
                    .Include(d => d.Samples)
                    .FirstOrDefaultAsync(d => d.Date == dailyHeart.Date);

                if (existing is not null)
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

        // TODO proper handling of same timestamp data, either all local or all gmt, 

        // Upserts DailySleep with it's related HeartRateSamples
        public async Task SaveDailySleep(DailySleep dailySleep, List<GarminTimeSampleDto> heartRates)
        {
            if (dailySleep is null)
                return;

            DateOnly date = dailySleep.Date;

            try
            {
                var existing = await _context.DailySleep.FirstOrDefaultAsync(x => x.Date == date);

                // TODO handle multiple sleep per day, checkout naps too 
                // update existing values
                if (existing is not null)
                {
                    existing.SleepTimeSeconds = dailySleep.SleepTimeSeconds;
                    existing.DeepSleepSeconds = dailySleep.DeepSleepSeconds;
                    existing.LightSleepSeconds = dailySleep.LightSleepSeconds;
                    existing.RemSleepSeconds = dailySleep.RemSleepSeconds;
                    existing.AwakeSleepSeconds = dailySleep.AwakeSleepSeconds;
                    existing.AvgSleepStress = dailySleep.AvgSleepStress;
                    existing.AvgHeartRate = dailySleep.AvgHeartRate;
                    //existing.RestingHeartRate = dailySleep.RestingHeartRate;
                    //existing.AvgOvernightHrv = dailySleep.AvgOvernightHrv;
                }
                else
                {
                    _context.DailySleep.Add(dailySleep);
                }

                // map incoming DTOs into a lookup dictionary by timestamp
                var incomingSamples = heartRates?.ToDictionary(
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v.StartGmt), //TODO timezones
                    v => v.Value
                ) ?? new Dictionary<DateTimeOffset, int>();

                if (incomingSamples.Count > 0)
                {
                    var timestamps = incomingSamples.Keys.ToList();

                    // find any existing HeartRateSamples in DB by timestamp
                    var existingSamples = await _context.HeartRateSample
                        .Where(s => s.Date == date && timestamps.Contains(s.Timestamp))
                        .ToListAsync();

                    // update and mark existing records as sleeping
                    foreach (var sample in existingSamples)
                    {
                        sample.Sleeping = true;
                        incomingSamples.Remove(sample.Timestamp); // remove rows we updated from sleep heartrate samples, probably unneccsary but better safe than sorry for now
                    }

                    // TODO probably unneccsary since heart rate already gets the same by default?
                    // create new HeartRateSamples for the ones that werent already in DB
                    var newSamples = incomingSamples.Select(kvp => new HeartRateSample
                    {
                        Date = date,
                        Timestamp = kvp.Key,
                        BPM = kvp.Value,
                        Sleeping = true
                    }).ToList();

                    if (newSamples.Count > 0)
                    {
                        // TODO if dailyheartrate for given date doesnt exist "yet" while fetching sleep for that date first we get error so always fetch dailyheart first but still catch error evne though it prob would never happen with how data would properly be fetched in order with normal use
                        _context.HeartRateSample.AddRange(newSamples);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving DailySleep for {Date}", dailySleep.Date);
                throw;
            }
        }

        public async Task SaveDailyStress(DailyStress dailyStress)
        {
            if (dailyStress is null)
                return;

            try
            {
                var existing = await _context.DailyStress.FirstOrDefaultAsync(d => d.Date == dailyStress.Date);

                // Update record if already exists
                if (existing is not null)
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
                _logger.LogError(ex, "Database error occurred while trying to save/update DailyStress for date {Date}.", dailyStress.Date);
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
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(v[0]!.Value), // TODO fix timezones
                    BPM = (int)v[1]!.Value
                }).ToList() ?? new List<HeartRateSample>()
        };

        private static DailyStress MapToEntity(DailyStressDto dto) => new()
        {
            Date = dto.CalendarDate,
            Average = dto.AvgStressLevel,
            Max = dto.MaxStressLevel,
        };

        private static DailySleep MapToEntity(SleepResponseDto dto)
        {
            var d = dto.DailySleep;
            var date = d.CalendarDate;

            return new DailySleep
            {
                Date = date,
                SleepTimeSeconds = d.SleepTimeSeconds,
                DeepSleepSeconds = d.DeepSleepSeconds,
                LightSleepSeconds = d.LightSleepSeconds,
                RemSleepSeconds = d.RemSleepSeconds,
                AwakeSleepSeconds = d.AwakeSleepSeconds,
            AvgHeartRate = (int)d.AvgHeartRate,
            AvgSleepStress = (int)d.AvgSleepStress,
            };
    }

    }

