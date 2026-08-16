namespace LifeTracker
{
    public static class ConfigurationExtensions
    {
        public static Uri GetRequiredUri(this IConfiguration config, string key) =>
            new Uri(config[key] ?? throw new InvalidOperationException($"Configuration key '{key}' is missing."));
    }
}
