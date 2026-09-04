using LifeTracker.Dtos.Garmin;
using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;
using static LifeTracker.Mappers.GarminMapping;

namespace LifeTracker.Services;

public partial class GarminBridgeService
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

    #region Getters
    public async Task<DailyHeartRate?> GetHeartRateByDay(DateOnly date) =>
        await _context.DailyHeartRates.AsNoTracking().Include(d => d.Samples).FirstOrDefaultAsync(d => d.Date == date);

    public async Task<DailyStress?> GetStressByDay(DateOnly date) =>
        await _context.DailyStresses.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

    public async Task<DailySleep?> GetSleepByDay(DateOnly date) =>
        await _context.DailySleeps.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

    public async Task<object?> GarminBridgeHealthCheck() =>
        await _httpClient.GetFromJsonAsync<object?>("health");


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

    public async Task<IReadOnlyList<GarminDay>> GetAllGarminDays()
    {
        var heartRates = await _context.DailyHeartRates.AsNoTracking().Include(d => d.Samples).ToListAsync();
        var stresses = await _context.DailyStresses.AsNoTracking().ToListAsync();
        var sleeps = await _context.DailySleeps.AsNoTracking().ToListAsync();

        // index records by date for fast lookup and combining
        var heartByDate = heartRates.ToDictionary(x => x.Date);
        var stressByDate = stresses.ToDictionary(x => x.Date);
        var sleepByDate = sleeps.ToDictionary(x => x.Date);

        // get all possible dates across the entities, newest to oldest
        var dates = heartByDate.Keys.Union(stressByDate.Keys).Union(sleepByDate.Keys).OrderByDescending(d => d);

        var days = new List<GarminDay>();
        foreach (var date in dates)
        {
            heartByDate.TryGetValue(date, out var heart);
            stressByDate.TryGetValue(date, out var stress);
            sleepByDate.TryGetValue(date, out var sleep);

            // skip entries where both heart and stress are null, sleep can be nullable so its not required
            if (heart is null && stress is null)
                continue;

            days.Add(new GarminDay(date, heart, stress, sleep));
        }

        return days;
    }
    #endregion

    #region Bridge & Sync

    // TODO turn into background task or something and cancellation tokens? since its such a long duration function
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
                GarminDay? day = await SyncAllDataByDay(date);
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
    // Helper function for all sync functions
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

    // Syncs all Garmin data from the official API via the python GarminConnect bridge and upserts into DB
    public async Task<GarminDay?> SyncAllDataByDay(DateOnly date)
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

    public async Task<DailySleep?> SyncSleepByDay(DateOnly date)
    {
        var sleepDTO = await FetchFromBridgeAsync<SleepResponseDto>("sleep", date);
        if (sleepDTO is null)
            return null;

        var dailySleep = MapToEntity(sleepDTO);
        await SaveDailySleep(dailySleep, sleepDTO.SleepHeartRate);
        return dailySleep;
    }
    #endregion
}
