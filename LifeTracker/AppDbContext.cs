using LifeTracker.Entities;
using LifeTracker.Entities.ESP32;
using LifeTracker.Entities.ActivityWatch;
using LifeTracker.Entities.Garmin;
using LifeTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeTracker;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BuienradarStationMeasurement> BuienradarStationMeasurements { get; set; }
    public DbSet<RoomClimateMeasurement> RoomClimateMeasurements { get; set; }
    public DbSet<ActivityEvent> ActivityWatchEvents { get; set; }
    public DbSet<HeartRateSample> HeartRateSamples { get; set; }
    public DbSet<DailyHeartRate> DailyHeartRates { get; set; }
    public DbSet<DailyStress> DailyStresses { get; set; }
    public DbSet<DailySleep> DailySleeps { get; set; }

    // TODO proper db health checks, checking if schemas are properly setup even if db is running can still fail if schema is not setup properly
    // TODO account stuff, rn its just single user hardcoded, stuff like that should be in a separate table and linked to the data, so multiple users can use the same db, "updatedby/createdby", account id linked to every or the proper db entries and such

    public override int SaveChanges()
    {
        SetAuditProperties();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Sets CreatedAt and UpdatedAt properties for all derived BaseEntities
    private void SetAuditProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // descending index for timestamp since its used for fetching only new events
        modelBuilder.Entity<ActivityEvent>().HasIndex(e => e.Timestamp).IsDescending();

        modelBuilder.Entity<DailyHeartRate>().HasKey(d => d.Date);
        modelBuilder.Entity<DailyStress>().HasKey(d => d.Date);
        modelBuilder.Entity<DailySleep>().HasKey(d => d.Date);


        modelBuilder.Entity<HeartRateSample>(e =>
        {
            e.HasKey(s => new { s.Date, s.Timestamp }); // composite PK

            // 1 DailyHeartRate to many HeartRateSamples 
            e.HasOne(s => s.DailyHeartRate)
                .WithMany(d => d.Samples)
                .HasForeignKey(s => s.Date)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Adjust column order in DB due to ClimateMeasurement already being an extension of BaseEntity 
        modelBuilder.Entity<RoomClimateMeasurement>(entity =>
        {
            entity.Property(e => e.ID).HasColumnOrder(1);
            entity.Property(e => e.Timestamp).HasColumnOrder(2);
            entity.Property(e => e.CO2).HasColumnOrder(3);
            entity.Property(e => e.Temperature).HasColumnOrder(4);
            entity.Property(e => e.Humidity).HasColumnOrder(5);
            entity.Property(e => e.CreatedAt).HasColumnOrder(6);
            entity.Property(e => e.UpdatedAt).HasColumnOrder(7);
        });

        modelBuilder.Entity<BuienradarStationMeasurement>(entity =>
        {
            entity.Property(e => e.ID).HasColumnOrder(1);
            entity.Property(e => e.StationId).HasColumnOrder(2);
            entity.Property(e => e.StationName).HasColumnOrder(3);
            entity.Property(e => e.WeatherDescription).HasColumnOrder(4);
            entity.Property(e => e.Temperature).HasColumnOrder(5);
            entity.Property(e => e.Humidity).HasColumnOrder(6);
            entity.Property(e => e.WindDirection).HasColumnOrder(7);
            entity.Property(e => e.Precipitation).HasColumnOrder(8);
            entity.Property(e => e.SunPower).HasColumnOrder(9);
            entity.Property(e => e.RainFallLastHour).HasColumnOrder(10);
            entity.Property(e => e.RainFallLast24Hour).HasColumnOrder(11);
            entity.Property(e => e.WindspeedBft).HasColumnOrder(12);
            entity.Property(e => e.AirPressure).HasColumnOrder(13);
            entity.Property(e => e.CreatedAt).HasColumnOrder(15);
            entity.Property(e => e.UpdatedAt).HasColumnOrder(16);
        });

    }

}
