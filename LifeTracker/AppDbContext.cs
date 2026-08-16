using Microsoft.EntityFrameworkCore;

namespace LifeTracker
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StationMeasurement> WeatherLogs { get; set; }
        public DbSet<ActivityEvent> ActivityWatchEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
