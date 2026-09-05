namespace LifeTracker.Tests.Garmin;

sealed class BridgeHandler : HttpMessageHandler
{
    // Stub HTTP responses of the routes for external API during testing
    public static Dictionary<string, HttpResponseMessage> Responses { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var key = req.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty;

        if (Responses.TryGetValue(key, out var response))
        {
            return Task.FromResult(response);
        }

        // Fail fast instead of false negative / guessing a default status code
        throw new InvalidOperationException($"No mock response configured for bridge request: {key}");
    }
}
