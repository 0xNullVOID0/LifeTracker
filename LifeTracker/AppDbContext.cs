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

            // descending index for timestamp since its used for fetching only new events
            modelBuilder.Entity<ActivityEvent>()
                .HasIndex(e => e.Timestamp)
                .IsDescending();

            modelBuilder.Entity<ActivityEvent>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<StationMeasurement>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

        }

    }
}
