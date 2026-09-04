using System.Security.Cryptography;
using System.Text;
using LifeTracker.Configuration;
using Microsoft.Extensions.Options;

namespace LifeTracker.Filters;

public static class DeviceKeyFilter
{
    public static async ValueTask<object?> Require(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var opts = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<ESP32Options>>().Value;

        if (string.IsNullOrWhiteSpace(opts.APIkey) || string.IsNullOrWhiteSpace(opts.DeviceID))
            return Results.Json(new { error = "ESP32 ingest is not configured" }, statusCode: StatusCodes.Status503ServiceUnavailable);

        var deviceID = ctx.HttpContext.Request.Headers["X-Device-ID"].ToString();
        var apiKey = ctx.HttpContext.Request.Headers["X-API-Key"].ToString();

        var idOk = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(deviceID),
            Encoding.UTF8.GetBytes(opts.DeviceID));

        var keyOk = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(apiKey),
            Encoding.UTF8.GetBytes(opts.APIkey));

        if (!idOk || !keyOk)
            return Results.Json(new { error = "Invalid device credentials" }, statusCode: StatusCodes.Status401Unauthorized);

        return await next(ctx);
    }

    // FixedTimeEquals throws if lengths differ
    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        if (a.Length != b.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
