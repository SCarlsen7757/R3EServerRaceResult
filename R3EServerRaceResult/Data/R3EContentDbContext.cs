using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models.R3EContent;

namespace R3EServerRaceResult.Data;

public class R3EContentDbContext : DbContext
{
    public R3EContentDbContext(DbContextOptions<R3EContentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
    public DbSet<CarClass> CarClasses { get; set; } = null!;
    public DbSet<Car> Cars { get; set; } = null!;
    public DbSet<Livery> Liveries { get; set; } = null!;
    public DbSet<Track> Tracks { get; set; } = null!;
    public DbSet<Layout> Layouts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<CarClass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            
            entity.HasOne(e => e.Class)
                .WithMany(c => c.Cars)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Manufacturer)
                .WithMany(m => m.Cars)
                .HasForeignKey(e => e.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.ManufacturerId);
        });

        modelBuilder.Entity<Livery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsDefault).IsRequired();
            
            entity.HasOne(e => e.Car)
                .WithMany(c => c.Liveries)
                .HasForeignKey(e => e.CarId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.CarId);
        });

        modelBuilder.Entity<Track>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Layout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MaxVehicles).IsRequired();
            
            entity.HasOne(e => e.Track)
                .WithMany(t => t.Layouts)
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.TrackId);
        });
    }
}
