using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace LifeTracker
{
    public class ActivityWatchService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public ActivityWatchService(HttpClient httpclient, AppDbContext context)
        {
            _httpClient = httpclient;
            _context = context;
        }

        public async Task<List<ActivityEvent>> GetBucketEvents()
        {
            // TODO make generic instead of hardcoded, use config/env file for proper bucket ID
            return await GetBucketEvents("aw-watcher-window_X3D");
        }

        public async Task<List<ActivityEvent>> GetBucketEvents(string bucketID)
        {
            string url = $"http://localhost:5600/api/0/buckets/{bucketID}/events";

            // TODO error handling

            // Get JSON activity events from local ActivityWatch API for the specific bucket ID
            var events_dtos = await _httpClient.GetFromJsonAsync<List<ActivityEventDto>>(url);

            if (events_dtos == null || events_dtos.Count == 0)
                return new List<ActivityEvent>();

            // map DTO's to database entity
            var events = events_dtos.Select(dto => new ActivityEvent
            {
                AwID = dto.ID,
                Timestamp = dto.Timestamp.ToUniversalTime(), // needs to be universal time for postgres otherwise wont accept, error
                Duration = dto.Duration,
                App = dto.Data.App ?? string.Empty,
                Title = dto.Data.Title ?? string.Empty
            }).ToList();

            // test save
            if (events != null && events.Count > 0)
            {
                await SaveEventsAsync(events);
            }

            return events;
        }

        public async Task SaveEventsAsync(List<ActivityEvent> events)
        {

            // TODO prevent duplicate saving, check if incoming awID's already exist and use timestamps to only fetch new events to begin with

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
