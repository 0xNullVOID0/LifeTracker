namespace LifeTracker.Configuration;

public sealed class JwtOptions
{
    public const string Section = "JWT";
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "LifeTrackerAPI";
    public string Audience { get; set; } = "LifeTrackerClient";
    public string Password { get; set; } = "";
}
