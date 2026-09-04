using LifeTracker.Configuration;
using LifeTracker.Dtos.ActivityWatch;
using LifeTracker.Entities.ActivityWatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeTracker.Services;

public class ActivityWatchService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ActivityWatchOptions _options;
    private readonly ILogger<ActivityWatchService> _logger;

    public ActivityWatchService(HttpClient httpclient, AppDbContext context, IOptions<ActivityWatchOptions> options, ILogger<ActivityWatchService> logger)
    {
        _httpClient = httpclient;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<ActivityEvent>?> GetEvents() =>
        await _context.ActivityWatchEvents.AsNoTracking().ToListAsync();


    // Query from start to optional end timestamp
    public async Task<List<ActivityEvent>?> GetEvents(DateTimeOffset start, DateTimeOffset? end = null)
    {
        var query = _context.ActivityWatchEvents.AsNoTracking().Where(e => e.Timestamp >= start);

        // only query range with end if end timestamp passed
        if (end.HasValue)
            query = query.Where(e => e.Timestamp <= end.Value);

        var events = await query.OrderBy(e => e.Timestamp).ToListAsync();
        return events.Count != 0 ? events : null;
    }


    public async Task<List<ActivityEvent>?> SyncBucketEvents()
    {
        return await SyncBucketEvents(_options.BucketID);
    }

    public async Task<List<ActivityEvent>?> SyncBucketEvents(string bucketID)
    {
        string url = $"buckets/{bucketID}/events";

        // Get JSON activity events from local ActivityWatch API for the specific bucket ID
        var events_dtos = await _httpClient.GetFromJsonAsync<List<ActivityEventDto>>(url);

        if (events_dtos is null || events_dtos.Count == 0)
            return null;

        // map DTO's to database entity
        var events = events_dtos.Select(MapToEntity).ToList();

        // test save
        if (events is not null && events.Count > 0)
        {
            await SaveEventsAsync(events);
        }

        return events;
    }

    public async Task<List<ActivityEvent>?> SyncNewBucketEvents()
    {
        return await SyncNewBucketEvents(_options.BucketID);
    }

    public async Task<List<ActivityEvent>?> SyncNewBucketEvents(string bucketID)
    {
        // find the timestamp of the last(newest) event in DB
        var latestTimestamp = await _context.ActivityWatchEvents
            .OrderByDescending(e => e.Timestamp)
            .Select(e => (DateTime?)e.Timestamp)
            .FirstOrDefaultAsync();

        string url = $"buckets/{bucketID}/events";

        // skips this if no timestamp cause there are no entries yet and then just fetches all existing entries
        if (latestTimestamp.HasValue)
        {
            // 'o' format ensures standard ISO 8601 string representation required by the API
            string isoStartTime = latestTimestamp.Value.ToUniversalTime().ToString("o");
            // append starting timestamp to only fetch new events that aren't in DB yet
            url += $"?start={Uri.EscapeDataString(isoStartTime)}";
        }

        var events_dtos = await _httpClient.GetFromJsonAsync<List<ActivityEventDto>>(url);

        // check if new events exist or not
        if (events_dtos is null || events_dtos.Count == 0)
            return null;

        // map DTOs to DB entity
        var events = events_dtos.Select(MapToEntity).ToList();

        // add new events to DB
        await SaveEventsAsync(events);

        return events;
    }

    public async Task SaveEventsAsync(List<ActivityEvent> events)
    {
        if (events is null || events.Count == 0)
            return;

        // fetch existing IDs from the incoming batch to prevent duplicate primary key exceptions
        var incomingIDs = events.Select(d => d.AwID).ToList();
        var existingIDs = await _context.ActivityWatchEvents
            .Where(e => incomingIDs.Contains(e.AwID))
            .Select(e => e.AwID)
            .ToListAsync();

        // filter out possible duplicates
        events = events
            .Where(dto => !existingIDs.Contains(dto.ID))
            .ToList();

        // check if there are still events left after filter(probably would never be empty but just in case)
        if (events is null || events.Count == 0)
            return;

        try
        {
            _context.ActivityWatchEvents.AddRange(events);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while trying to save ActivityWatch events.");
            throw;
        }
    }

    // helper function to remove redundant/repeated DTO to entity mapping
    private static ActivityEvent MapToEntity(ActivityEventDto dto) => new()
    {
        AwID = dto.ID,
        Timestamp = dto.Timestamp.ToUniversalTime(), // needs to be universal time for postgres otherwise wont accept and gives error
        Duration = dto.Duration,
        App = dto.Data.App ?? string.Empty,
        Title = dto.Data.Title ?? string.Empty
    };
}
