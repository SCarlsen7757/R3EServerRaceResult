using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models;

namespace R3EServerRaceResult.Data
{
    public class ChampionshipDbContext : DbContext
    {
        public ChampionshipDbContext(DbContextOptions<ChampionshipDbContext> options)
            : base(options)
        {
        }

        public DbSet<ChampionshipConfiguration> ChampionshipConfigurations { get; set; }
        public DbSet<RaceCountState> RaceCountStates { get; set; }
        public DbSet<Summary> SummaryFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChampionshipConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasMaxLength(36);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.EndDate)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Ignore computed properties
                entity.Ignore(e => e.IsActive);
                entity.Ignore(e => e.IsExpired);

                // Indexes for efficient date range queries
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.EndDate);
                entity.HasIndex(e => new { e.StartDate, e.EndDate });
            });

            modelBuilder.Entity<RaceCountState>(entity =>
            {
                entity.HasKey(e => e.Year);

                entity.Property(e => e.Year)
                    .IsRequired();

                entity.Property(e => e.RaceCount)
                    .IsRequired();

                entity.Property(e => e.RacesPerChampionship)
                    .IsRequired();

                entity.Property(e => e.LastUpdated)
                    .IsRequired();
            });

            modelBuilder.Entity<Summary>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasMaxLength(36);

                entity.Property(e => e.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.ChampionshipKey)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ChampionshipName)
                    .HasMaxLength(200);

                entity.Property(e => e.Strategy)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(e => e.Year)
                    .IsRequired();

                entity.Property(e => e.RaceCount)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.LastUpdated)
                    .IsRequired();

                // Unique constraint on FilePath
                entity.HasIndex(e => e.FilePath)
                    .IsUnique();

                // Indexes for efficient queries
                entity.HasIndex(e => e.Year);
                entity.HasIndex(e => e.Strategy);
                entity.HasIndex(e => e.ChampionshipKey);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.RaceCount);
            });
        }
    }
}
