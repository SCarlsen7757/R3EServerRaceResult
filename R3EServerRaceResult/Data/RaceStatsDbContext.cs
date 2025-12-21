using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models.RaceStats;

namespace R3EServerRaceResult.Data;

public class RaceStatsDbContext : DbContext
{
    public RaceStatsDbContext(DbContextOptions<RaceStatsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Driver> Drivers { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<RaceSession> Sessions { get; set; } = null!;
    public DbSet<RaceResult> Results { get; set; } = null!;
    public DbSet<Lap> Laps { get; set; } = null!;
    public DbSet<RaceIncident> Incidents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // User ID from R3E
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TrackId).IsRequired();
            entity.Property(e => e.LayoutId).IsRequired();
            entity.Property(e => e.EventDate).IsRequired();
            entity.Property(e => e.ServerName).IsRequired().HasMaxLength(200);

            entity.HasIndex(e => e.TrackId);
            entity.HasIndex(e => e.LayoutId);
            entity.HasIndex(e => e.EventDate);
        });

        modelBuilder.Entity<RaceSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.SessionType).IsRequired().HasConversion<string>();
            entity.Property(e => e.SessionNumber).IsRequired();

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Sessions)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.SessionType);
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).IsRequired();
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.CarId).IsRequired();
            entity.Property(e => e.StartPosition).IsRequired();
            entity.Property(e => e.Position).IsRequired();
            entity.Property(e => e.ClassStartPosition).IsRequired();
            entity.Property(e => e.ClassPosition).IsRequired();
            entity.Property(e => e.TotalRaceTime).IsRequired();
            entity.Property(e => e.BestLapTime).IsRequired(false); // Nullable - no valid lap
            entity.Property(e => e.FinishStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalLaps).IsRequired();

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Results)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Results)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CarId);
            entity.HasIndex(e => e.Position);
            entity.HasIndex(e => e.BestLapTime); // Index for fastest lap queries
        });

        modelBuilder.Entity<Lap>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).IsRequired();
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.LapNumber).IsRequired();
            entity.Property(e => e.LapTime).IsRequired(false); // Nullable - incomplete lap
            entity.Property(e => e.Sector1Time).IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.Sector2Time).IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.Sector3Time).IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.IsValid).IsRequired();

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Laps)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Laps)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => new { e.SessionId, e.DriverId, e.LapNumber });
            entity.HasIndex(e => e.LapTime); // Index for fastest lap queries
        });

        modelBuilder.Entity<RaceIncident>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).IsRequired();
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.IncidentType).IsRequired().HasConversion<string>();
            entity.Property(e => e.IncidentPoints).IsRequired();
            entity.Property(e => e.LapNumber).IsRequired();
            entity.Property(e => e.InvolvedDriverId).IsRequired(false);

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Incidents)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Incidents)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvolvedDriver)
                .WithMany(d => d.InvolvedIncidents)
                .HasForeignKey(e => e.InvolvedDriverId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.DriverId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.IncidentType);
            entity.HasIndex(e => e.InvolvedDriverId);
        });
    }
}
