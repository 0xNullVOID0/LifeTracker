using Microsoft.EntityFrameworkCore;
using LifeTracker.Entities.Garmin;
using LifeTracker.Dtos.Garmin;

namespace LifeTracker.Services;

// Partial class to separate persistence logic and make it less cluttered
public partial class GarminBridgeService
{
    // Upserts DailyHeartRate with it's related HeartRateSamples
    public async Task SaveDailyHeartRate(DailyHeartRate dailyHeart)
    {
        if (dailyHeart is null)
            return;

        try
        {
            // Check if a record already exists for this date including possible child HeartRateSamples
            var existing = await _context.DailyHeartRates
                .Include(d => d.Samples)
                .FirstOrDefaultAsync(d => d.Date == dailyHeart.Date);

            if (existing is not null)
            {
                // Update summary properties
                existing.RestingRate = dailyHeart.RestingRate;
                existing.Min = dailyHeart.Min;
                existing.Max = dailyHeart.Max;

                // Delete old samples from the context to prevent orphaned records/foreign key conflicts
                _context.HeartRateSamples.RemoveRange(existing.Samples);

                // Attach the new sample list
                existing.Samples = dailyHeart.Samples;
            }
            else
            {
                // Insert new entity along with its samples
                _context.DailyHeartRates.Add(dailyHeart);
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
            var existing = await _context.DailySleeps.FirstOrDefaultAsync(x => x.Date == date);

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
                _context.DailySleeps.Add(dailySleep);
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
                var existingSamples = await _context.HeartRateSamples
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
                    _context.HeartRateSamples.AddRange(newSamples);
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
            var existing = await _context.DailyStresses.FirstOrDefaultAsync(d => d.Date == dailyStress.Date);

            // Update record if already exists
            if (existing is not null)
            {
                existing.Average = dailyStress.Average;
                existing.Max = dailyStress.Max;
                // TODO add updatedAt
            }
            else
            {
                _context.DailyStresses.Add(dailyStress);
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while trying to save/update DailyStress for date {Date}.", dailyStress.Date);
            throw;
        }
    }
}
