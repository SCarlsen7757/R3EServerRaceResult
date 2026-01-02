using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models.R3EContent;

namespace R3EServerRaceResult.Data;

public class R3EContentDbContext : DbContext
{
    public const string SchemaName = "r3e_content";

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

        // Use snake_case schema
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.ToTable("manufacturers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_manufacturers_name");
        });

        modelBuilder.Entity<CarClass>(entity =>
        {
            entity.ToTable("car_classes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_car_classes_name");
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.ToTable("cars");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ManufacturerId).HasColumnName("manufacturer_id");

            entity.HasOne(e => e.Class)
                .WithMany(c => c.Cars)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cars_car_classes");

            entity.HasOne(e => e.Manufacturer)
                .WithMany(m => m.Cars)
                .HasForeignKey(e => e.ManufacturerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cars_manufacturers");

            entity.HasIndex(e => e.Id).HasDatabaseName("ix_cars_id");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_cars_name");
            entity.HasIndex(e => e.ClassId).HasDatabaseName("ix_cars_class_id");
            entity.HasIndex(e => e.ManufacturerId).HasDatabaseName("ix_cars_manufacturer_id");
        });

        modelBuilder.Entity<Livery>(entity =>
        {
            entity.ToTable("liveries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.IsDefault).HasColumnName("is_default").IsRequired();
            entity.Property(e => e.CarId).HasColumnName("car_id");

            entity.HasOne(e => e.Car)
                .WithMany(c => c.Liveries)
                .HasForeignKey(e => e.CarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_liveries_cars");

            entity.HasIndex(e => e.CarId).HasDatabaseName("ix_liveries_car_id");
        });

        modelBuilder.Entity<Track>(entity =>
        {
            entity.ToTable("tracks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_tracks_name");
        });

        modelBuilder.Entity<Layout>(entity =>
        {
            entity.ToTable("layouts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.MaxVehicles).HasColumnName("max_vehicles").IsRequired();
            entity.Property(e => e.TrackId).HasColumnName("track_id");

            entity.HasOne(e => e.Track)
                .WithMany(t => t.Layouts)
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_layouts_tracks");

            entity.HasIndex(e => e.TrackId).HasDatabaseName("ix_layouts_track_id");
        });
    }
}
