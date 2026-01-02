using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models.RaceStats;

namespace R3EServerRaceResult.Data;

public class RaceStatsDbContext : DbContext
{
    public const string SchemaName = "race_stats";

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

        // Use snake_case schema
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("drivers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever(); // User ID from R3E
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_drivers_name");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.TrackId).HasColumnName("track_id").IsRequired();
            entity.Property(e => e.LayoutId).HasColumnName("layout_id").IsRequired();
            entity.Property(e => e.EventDate).HasColumnName("event_date").IsRequired();
            entity.Property(e => e.ServerName).HasColumnName("server_name").IsRequired().HasMaxLength(200);

            entity.HasIndex(e => e.TrackId).HasDatabaseName("ix_events_track_id");
            entity.HasIndex(e => e.LayoutId).HasDatabaseName("ix_events_layout_id");
            entity.HasIndex(e => e.EventDate).HasDatabaseName("ix_events_event_date");
        });

        modelBuilder.Entity<RaceSession>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.SessionType).HasColumnName("session_type").IsRequired().HasConversion<string>();
            entity.Property(e => e.SessionNumber).HasColumnName("session_number").IsRequired();

            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Sessions)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sessions_events");

            entity.HasIndex(e => e.EventId).HasDatabaseName("ix_sessions_event_id");
            entity.HasIndex(e => e.SessionType).HasDatabaseName("ix_sessions_session_type");
            
            // Unique constraint: one session of each type/number per event
            entity.HasIndex(e => new { e.EventId, e.SessionType, e.SessionNumber })
                .IsUnique()
                .HasDatabaseName("ix_sessions_event_type_number");
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.ToTable("results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).HasColumnName("driver_id").IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
            entity.Property(e => e.CarId).HasColumnName("car_id").IsRequired();
            entity.Property(e => e.StartPosition).HasColumnName("start_position").IsRequired();
            entity.Property(e => e.Position).HasColumnName("position").IsRequired();
            entity.Property(e => e.ClassStartPosition).HasColumnName("class_start_position").IsRequired();
            entity.Property(e => e.ClassPosition).HasColumnName("class_position").IsRequired();
            entity.Property(e => e.TotalRaceTime).HasColumnName("total_race_time").IsRequired();
            entity.Property(e => e.BestLapTime).HasColumnName("best_lap_time").IsRequired(false); // Nullable - no valid lap
            entity.Property(e => e.FinishStatus).HasColumnName("finish_status").IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalLaps).HasColumnName("total_laps").IsRequired();

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Results)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_results_drivers");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Results)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_results_sessions");

            entity.HasIndex(e => e.DriverId).HasDatabaseName("ix_results_driver_id");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("ix_results_session_id");
            entity.HasIndex(e => e.CarId).HasDatabaseName("ix_results_car_id");
            entity.HasIndex(e => e.Position).HasDatabaseName("ix_results_position");
            entity.HasIndex(e => e.BestLapTime).HasDatabaseName("ix_results_best_lap_time"); // Index for fastest lap queries
        });

        modelBuilder.Entity<Lap>(entity =>
        {
            entity.ToTable("laps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).HasColumnName("driver_id").IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
            entity.Property(e => e.LapNumber).HasColumnName("lap_number").IsRequired();
            entity.Property(e => e.LapTime).HasColumnName("lap_time").IsRequired(false); // Nullable - incomplete lap
            entity.Property(e => e.Sector1Time).HasColumnName("sector_1_time").IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.Sector2Time).HasColumnName("sector_2_time").IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.Sector3Time).HasColumnName("sector_3_time").IsRequired(false); // Nullable - incomplete sector
            entity.Property(e => e.IsValid).HasColumnName("is_valid").IsRequired();

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Laps)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_laps_drivers");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Laps)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_laps_sessions");

            entity.HasIndex(e => e.DriverId).HasDatabaseName("ix_laps_driver_id");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("ix_laps_session_id");
            entity.HasIndex(e => new { e.SessionId, e.DriverId, e.LapNumber }).HasDatabaseName("ix_laps_session_driver_lap");
            entity.HasIndex(e => e.LapTime).HasDatabaseName("ix_laps_lap_time"); // Index for fastest lap queries
        });

        modelBuilder.Entity<RaceIncident>(entity =>
        {
            entity.ToTable("incidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.DriverId).HasColumnName("driver_id").IsRequired();
            entity.Property(e => e.SessionId).HasColumnName("session_id").IsRequired();
            entity.Property(e => e.IncidentType).HasColumnName("incident_type").IsRequired().HasConversion<string>();
            entity.Property(e => e.IncidentPoints).HasColumnName("incident_points").IsRequired();
            entity.Property(e => e.LapNumber).HasColumnName("lap_number").IsRequired();
            entity.Property(e => e.InvolvedDriverId).HasColumnName("involved_driver_id").IsRequired(false);

            entity.HasOne(e => e.Driver)
                .WithMany(d => d.Incidents)
                .HasForeignKey(e => e.DriverId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_incidents_drivers");

            entity.HasOne(e => e.Session)
                .WithMany(s => s.Incidents)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_incidents_sessions");

            entity.HasOne(e => e.InvolvedDriver)
                .WithMany(d => d.InvolvedIncidents)
                .HasForeignKey(e => e.InvolvedDriverId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_incidents_involved_drivers");

            entity.HasIndex(e => e.DriverId).HasDatabaseName("ix_incidents_driver_id");
            entity.HasIndex(e => e.SessionId).HasDatabaseName("ix_incidents_session_id");
            entity.HasIndex(e => e.IncidentType).HasDatabaseName("ix_incidents_incident_type");
            entity.HasIndex(e => e.InvolvedDriverId).HasDatabaseName("ix_incidents_involved_driver_id");
        });
    }
}
