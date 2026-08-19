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

            // composite primary key of foreign key and timestamp preventing duplicate sample timestamps per day
            modelBuilder.Entity<HeartRateSample>()
                .HasKey(s => new { s.Date, s.Timestamp });

        }

    }
}
