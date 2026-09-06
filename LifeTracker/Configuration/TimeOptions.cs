namespace LifeTracker.Configuration;

public sealed class TimeOptions
{
    public const string Section = "Time";
    public const string DefaultZoneID = "Europe/Amsterdam";
    
    public string ZoneID { get; set; } = DefaultZoneID;
}
