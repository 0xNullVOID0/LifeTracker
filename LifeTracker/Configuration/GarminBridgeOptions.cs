namespace LifeTracker.Configuration;

public sealed class GarminBridgeOptions
{
    public const string Section = "GarminBridge";
    public string ApiKey { get; set; } = "";
}
