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

        public async Task<List<ActivityEvent?>> GetBucketEvents()
        {
            // TODO make generic instead of hardcoded, use config/env file for proper bucket ID
            return await GetBucketEvents("aw-watcher-window_X3D");
        }

        public async Task<List<ActivityEvent?>> GetBucketEvents(string bucketID)
        {
            string url = $"http://localhost:5600/api/0/buckets/{bucketID}/events";

            // TODO error handling

            // Get JSON activity events from local ActivityWatch API for the specific bucket ID
            var events = await _httpClient.GetFromJsonAsync<List<ActivityEvent>>(url);
            return events;
        }
    }

 

    public class ActivityEvent
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("data")]
        public EventData Data { get; set; } = new();
    }

    public class EventData
    {
        [JsonPropertyName("app")]
        public string App { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
