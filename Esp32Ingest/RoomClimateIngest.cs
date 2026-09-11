using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace LifeTracker.Functions;

public class RoomClimateIngest
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public RoomClimateIngest(IHttpClientFactory factory, IConfiguration config)
    {
        _http = factory.CreateClient("LifeTracker");
        _config = config;
    }

    [Function("PostRoomClimate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "room-climate")] HttpRequest req)
    {
        var deviceId = req.Headers["X-Device-ID"].ToString();
        var apiKey = req.Headers["X-API-Key"].ToString();

        if (!FixedEquals(deviceId, _config["ESP32:DeviceID"]) ||
            !FixedEquals(apiKey, _config["ESP32:APIkey"]))
            return new UnauthorizedObjectResult(new { error = "Invalid device credentials" });

        using var forward = new HttpRequestMessage(HttpMethod.Post, "/api/room-climate")

        {
            Content = new StreamContent(req.Body)
        };
        forward.Content.Headers.ContentType = new("application/json");
        forward.Headers.TryAddWithoutValidation("X-Device-ID", deviceId);
        forward.Headers.TryAddWithoutValidation("X-API-Key", apiKey);

        var upstream = await _http.SendAsync(forward);
        var text = await upstream.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)upstream.StatusCode,
            Content = text,
            ContentType = "application/json"
        };
    }

    static bool FixedEquals(string? left, string? right)
    {
        var a = Encoding.UTF8.GetBytes(left ?? "");
        var b = Encoding.UTF8.GetBytes(right ?? "");
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
