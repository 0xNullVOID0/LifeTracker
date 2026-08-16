using LifeTracker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace LifeTracker
{
    public class ActivityWatchService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ActivityWatchSettings _settings;

        public ActivityWatchService(HttpClient httpclient, AppDbContext context, IOptions<ActivityWatchSettings> settings)
        {
            _httpClient = httpclient;
            _context = context;
            _settings = settings.Value;
        }

        public async Task<List<ActivityEvent>> FetchBucketEvents()
        {
            return await FetchBucketEvents(_settings.BucketID);
        }

        public async Task<List<ActivityEvent>> FetchBucketEvents(string bucketID)
        {
            string url = $"buckets/{bucketID}/events";

            // Get JSON activity events from local ActivityWatch API for the specific bucket ID
            var events_dtos = await _httpClient.GetFromJsonAsync<List<ActivityEventDto>>(url);

            if (events_dtos == null || events_dtos.Count == 0)
                return new List<ActivityEvent>();

            // map DTO's to database entity
            var events = events_dtos.Select(MapToEntity).ToList();

            // test save
            if (events != null && events.Count > 0)
            {
                await SaveEventsAsync(events);
            }

            return events;
        }

        public async Task<List<ActivityEvent>> FetchNewBucketEvents()
        {
            return await FetchNewBucketEvents(_settings.BucketID);
        }

        public async Task<List<ActivityEvent>> FetchNewBucketEvents(string bucketID)
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
            if (events_dtos == null || events_dtos.Count == 0)
                return new List<ActivityEvent>();

            // map DTOs to DB entity
            var events = events_dtos.Select(MapToEntity).ToList();

            // add new events to DB
            await SaveEventsAsync(events);

            return events;
        }

        public async Task SaveEventsAsync(List<ActivityEvent> events)
        {

            // TODO prevent duplicate saving, check if incoming awID's already exist

            try
            {
                _context.ActivityWatchEvents.AddRange(events);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("\n================== DATABASE ERROR ==================");
                Console.WriteLine(ex.InnerException?.Message ?? ex.Message);
                Console.WriteLine("===================================================\n");
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



    public class ActivityEvent
    {
        public int ID { get; set; }

        public int AwID { get; set; }
        public DateTime Timestamp { get; set; }
        public double Duration { get; set; }
        public string App { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class ActivityEventDto
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("data")]
        public EventDataDto Data { get; set; } = new();
    }

    public class EventDataDto
    {
        [JsonPropertyName("app")]
        public string App { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
