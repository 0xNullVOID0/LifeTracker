using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LifeTracker.Services.Background;

public class BuienradarBackgroundService : BackgroundService
{
    private readonly ILogger<BuienradarBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(2); // Check every X min TODO set interval in config or something


    public BuienradarBackgroundService(ILogger<BuienradarBackgroundService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("[Buienradar] Fetching local weather data");

                // temporary scope for safe DB use 
                using (var scope = _scopeFactory.CreateScope())
                {
                    var BuienradarService = scope.ServiceProvider.GetRequiredService<BuienradarService>();
                    await BuienradarService.GetBuienradarDataAsync();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Buienradar] ERROR during background service task");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }
}
