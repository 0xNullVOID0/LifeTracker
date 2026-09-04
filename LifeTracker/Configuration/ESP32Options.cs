namespace LifeTracker.Configuration;

public sealed class ESP32Options
{
    public const string Section = "ESP32";
    public string DeviceID { get; set; } = "";
    public string APIkey { get; set; } = "";
}
