using System.Net;
using LifeTracker.DTOs.Garmin;
using LifeTracker.Entities.Garmin;
using Microsoft.EntityFrameworkCore;
using static LifeTracker.Mappers.GarminMapping;

namespace LifeTracker.Services;

public partial class GarminBridgeService(
    HttpClient httpclient,
    AppDbContext context,
    ILogger<GarminBridgeService> logger)
{
    #region Getters

    public async Task<DailyHeartRate?> GetHeartRateByDay(DateOnly date) =>
        await context.DailyHeartRates.AsNoTracking().Include(d => d.Samples).FirstOrDefaultAsync(d => d.Date == date);

    public async Task<DailyStress?> GetStressByDay(DateOnly date) =>
        await context.DailyStresses.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

    public async Task<DailySleep?> GetSleepByDay(DateOnly date) =>
        await context.DailySleeps.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date);

    public async Task<object?> GarminBridgeHealthCheck() =>
        await httpclient.GetFromJsonAsync<object?>("health");


    // Gets all Garmin data from DB by date
    public async Task<GarminDay?> GetAllDataByDay(DateOnly date)
    {
        DailyHeartRate? heart = await GetHeartRateByDay(date);
        DailyStress? stress = await GetStressByDay(date);
        DailySleep? sleep = await GetSleepByDay(date);

        if (heart is null && stress is null && sleep is null)
            return null;

        return new GarminDay(date, heart, stress, sleep);
    }

    public async Task<IReadOnlyList<GarminDay>> GetAllGarminDays()
    {
        List<DailyHeartRate> heartRates =
            await context.DailyHeartRates.AsNoTracking().Include(d => d.Samples).ToListAsync();
        List<DailyStress> stresses = await context.DailyStresses.AsNoTracking().ToListAsync();
        List<DailySleep> sleeps = await context.DailySleeps.AsNoTracking().ToListAsync();

        // index records by date for fast lookup and combining
        var heartByDate = heartRates.ToDictionary(x => x.Date);
        var stressByDate = stresses.ToDictionary(x => x.Date);
        var sleepByDate = sleeps.ToDictionary(x => x.Date);

        // get all possible dates across the entities, newest to oldest
        var dates = heartByDate.Keys.Union(stressByDate.Keys).Union(sleepByDate.Keys).OrderByDescending(d => d);

        List<GarminDay> days = [];
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

    public async Task<bool> IsBridgeAvailable()
    {
        try
        {
            using var response = await httpclient.GetAsync("health");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    // TODO turn into background task or something and cancellation tokens? since its such a long duration function
    // Function for syncing larger stretches of Garming data such as for a first time setup
    public async Task<BackfillResult> SyncRecentDays(int days = 14)
    {
        days = Math.Clamp(days, 1,
            31); // cap backfill to 1 month to prevent overloading the Garmin API and getting rate limited. TODO add slower background task later for syncing long term profile data
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
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
                logger.LogWarning(ex, "Backfill stopped at {Date}", date);
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
        string url = $"{endpoint}?date={date:yyyy-MM-dd}";
        using var response = await httpclient.GetAsync(url); // fetch request with data(could be empty) from Python Garmin Bridge API

        if (response.StatusCode is HttpStatusCode.NoContent // 204 empty
                                or HttpStatusCode.NotFound) // 404 future
                                return default;

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Python Garmin Bridge error {(int)response.StatusCode} for {url}");

        return await response.Content.ReadFromJsonAsync<T>(); // deserialize the response's body to type T
    }

    // Syncs all Garmin data from the official API via the python GarminConnect bridge and upserts into DB
    public async Task<GarminDay?> SyncAllDataByDay(DateOnly date)
    {
        DailyHeartRate? heart = await SyncHeartRateByDay(date);
        DailyStress? stress = await SyncStressLevelByDay(date);
        DailySleep? sleep = await SyncSleepByDay(date);

        if (heart is null && stress is null && sleep is null)
            return null;

        return new GarminDay(date, heart, stress, sleep);
    }

    public async Task<DailyHeartRate?> SyncHeartRateByDay(DateOnly date)
    {
        var heartDTO = await FetchFromBridgeAsync<DailyHeartRateDTO>("heartrate", date);
        if (heartDTO is null)
            return null;

        DailyHeartRate dailyHeart = MapToEntity(heartDTO);
        await SaveDailyHeartRate(dailyHeart);
        return dailyHeart;
    }

    public async Task<DailyStress?> SyncStressLevelByDay(DateOnly date)
    {
        var stressDTO = await FetchFromBridgeAsync<DailyStressDTO>("stress", date);
        if (stressDTO is null)
            return null;

        DailyStress dailyStress = MapToEntity(stressDTO);
        await SaveDailyStress(dailyStress);
        return dailyStress;
    }

    public async Task<DailySleep?> SyncSleepByDay(DateOnly date)
    {
        var sleepDTO = await FetchFromBridgeAsync<SleepResponseDTO>("sleep", date);
        if (sleepDTO is null)
            return null;

        DailySleep dailySleep = MapToEntity(sleepDTO);
        await SaveDailySleep(dailySleep, sleepDTO.SleepHeartRate);
        return dailySleep;
    }

    #endregion
}
