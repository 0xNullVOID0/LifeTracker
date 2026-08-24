using LifeTracker.Entities.ActivityWatch;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<StationMeasurement> WeatherLogs { get; set; }
        public DbSet<ActivityEvent> ActivityWatchEvents { get; set; }
        public DbSet<HeartRateSample> HeartRateSample { get; set; }
        public DbSet<DailyHeartRate> DailyHeartRate { get; set; }
        public DbSet<DailyStress> DailyStress { get; set; }
        public DbSet<DailySleep> DailySleep { get; set; }

        // TODO proper db health checks, checking if schemas are properly setup even if db is running can still fail if schema is not setup properly

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // descending index for timestamp since its used for fetching only new events
            modelBuilder.Entity<ActivityEvent>()
                .HasIndex(e => e.Timestamp)
                .IsDescending();

            // TODO "automate", improve having to manually set createdate for every entity this way
            modelBuilder.Entity<ActivityEvent>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<StationMeasurement>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<DailyHeartRate>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<HeartRateSample>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()");

            modelBuilder.Entity<DailyStress>()
                      .Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");


            modelBuilder.Entity<DailyStress>()
                .HasKey(d => d.Date);

            modelBuilder.Entity<DailyHeartRate>()
                .HasKey(d => d.Date);

            modelBuilder.Entity<DailySleep>(e =>
            {
                e.HasKey(x => x.Date);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("NOW()");
            });

            modelBuilder.Entity<HeartRateSample>(e =>
            {
                e.HasKey(s => new { s.Date, s.Timestamp }); // composite PK
                e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");

                // 1 DailyHeartRate to many HeartRateSamples 
                e.HasOne(s => s.DailyHeartRate)
                    .WithMany(d => d.Samples)
                    .HasForeignKey(s => s.Date)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

        }

    }
}
