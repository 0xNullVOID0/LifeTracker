namespace LifeTracker.Configuration;

public class ActivityWatchOptions
{
    public const string SectionName = "APIs:ActivityWatch";

    public Uri BaseUrl { get; set; } = default!;
    public string BucketID { get; set; } = default!;
}
