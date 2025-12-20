using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Data;
using R3EServerRaceResult.Models.R3EContent;
using R3EServerRaceResult.Models.R3EContent.Json;
using System.Text.Json;

namespace R3EServerRaceResult.Services;

public class R3EContentDataSeeder
{
    private readonly R3EContentDbContext dbContext;
    private readonly ILogger<R3EContentDataSeeder> logger;
    private readonly string contentJsonPath;
    private readonly string manufacturersJsonPath;
    private readonly string tracksJsonPath;

    public R3EContentDataSeeder(
        R3EContentDbContext dbContext,
        ILogger<R3EContentDataSeeder> logger,
        IConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.logger = logger;

        var basePath = configuration.GetValue<string>("R3EContent:BasePath") ?? "Data/R3E";
        contentJsonPath = Path.Combine(basePath, "Content.json");
        manufacturersJsonPath = Path.Combine(basePath, "Manufacturers.json");
        tracksJsonPath = Path.Combine(basePath, "Tracks.json");
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Starting R3E content data seeding...");
            }

            // Check if data already exists
            var hasExistingData = await dbContext.Cars.AnyAsync(cancellationToken);

            if (hasExistingData)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Existing R3E content data found. Clearing database for reseeding...");
                }

                await ClearExistingDataAsync(cancellationToken);
            }

            // Parse JSON files
            var contentData = await ParseContentJsonAsync(cancellationToken);
            var manufacturersData = await ParseManufacturersJsonAsync(cancellationToken);
            var trackLocationsData = await ParseTrackLocationsJsonAsync(cancellationToken);

            // Seed data in transaction
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await SeedManufacturersAsync(manufacturersData, cancellationToken);
                await SeedCarsAndLiveriesAsync(contentData, manufacturersData, cancellationToken);
                await SeedTracksAndLayoutsAsync(contentData, trackLocationsData, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("R3E content data seeding completed successfully");
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error seeding R3E content data");
            }
            throw;
        }
    }

    private async Task ClearExistingDataAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Clearing existing R3E content data...");
        }

        // Delete in correct order due to foreign key constraints
        // Liveries depend on Cars
        dbContext.Liveries.RemoveRange(dbContext.Liveries);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Layouts depend on Tracks
        dbContext.Layouts.RemoveRange(dbContext.Layouts);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Cars depend on CarClasses and Manufacturers
        dbContext.Cars.RemoveRange(dbContext.Cars);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Tracks can be deleted independently
        dbContext.Tracks.RemoveRange(dbContext.Tracks);
        await dbContext.SaveChangesAsync(cancellationToken);

        // CarClasses and Manufacturers can be deleted now
        dbContext.CarClasses.RemoveRange(dbContext.CarClasses);
        dbContext.Manufacturers.RemoveRange(dbContext.Manufacturers);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Existing R3E content data cleared successfully");
        }
    }

    private async Task<ContentRootJson> ParseContentJsonAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(contentJsonPath))
        {
            throw new FileNotFoundException($"Content.json not found at: {contentJsonPath}");
        }

        var json = await File.ReadAllTextAsync(contentJsonPath, cancellationToken);
        var content = JsonSerializer.Deserialize<ContentRootJson>(json);

        if (content == null)
        {
            throw new InvalidOperationException("Failed to deserialize Content.json");
        }

        return content;
    }

    private async Task<ManufacturersRootJson> ParseManufacturersJsonAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(manufacturersJsonPath))
        {
            throw new FileNotFoundException($"Manufacturers.json not found at: {manufacturersJsonPath}");
        }

        var json = await File.ReadAllTextAsync(manufacturersJsonPath, cancellationToken);
        var manufacturers = JsonSerializer.Deserialize<ManufacturersRootJson>(json);

        if (manufacturers == null)
        {
            throw new InvalidOperationException("Failed to deserialize Manufacturers.json");
        }

        return manufacturers;
    }

    private async Task<TrackLocationsRootJson> ParseTrackLocationsJsonAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(tracksJsonPath))
        {
            throw new FileNotFoundException($"Tracks.json not found at: {tracksJsonPath}");
        }

        var json = await File.ReadAllTextAsync(tracksJsonPath, cancellationToken);
        var trackLocations = JsonSerializer.Deserialize<TrackLocationsRootJson>(json);

        if (trackLocations == null)
        {
            throw new InvalidOperationException("Failed to deserialize Tracks.json");
        }

        return trackLocations;
    }

    private async Task SeedManufacturersAsync(ManufacturersRootJson manufacturersData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeding {Count} manufacturers...", manufacturersData.Manufacturers.Count);
        }

        var manufacturers = manufacturersData.Manufacturers
            .Select(m => new Manufacturer
            {
                Id = m.Id,
                Name = m.Name,
                Country = m.Country
            })
            .ToList();

        await dbContext.Manufacturers.AddRangeAsync(manufacturers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} manufacturers", manufacturers.Count);
        }
    }

    private async Task SeedCarsAndLiveriesAsync(ContentRootJson contentData, ManufacturersRootJson manufacturersData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeding cars and liveries...");
        }

        // Create a lookup of CarId -> ManufacturerId
        var carToManufacturer = manufacturersData.Manufacturers
            .SelectMany(m => m.CarIds.Select(carId => new { CarId = carId, ManufacturerId = m.Id }))
            .ToDictionary(x => x.CarId, x => x.ManufacturerId);

        // Extract unique car classes
        var uniqueClasses = contentData.Liveries
            .Select(l => l.Car.Class)
            .Distinct()
            .Select(classId => new CarClass
            {
                Id = classId,
                Name = $"Class {classId}" // Generic name, can be updated later
            })
            .ToList();

        await dbContext.CarClasses.AddRangeAsync(uniqueClasses, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} car classes", uniqueClasses.Count);
        }

        // Create cars
        var cars = new List<Car>();
        var liveries = new List<Livery>();

        foreach (var carWithLiveries in contentData.Liveries)
        {
            var carJson = carWithLiveries.Car;

            // Skip if manufacturer not found
            if (!carToManufacturer.TryGetValue(carJson.Id, out var manufacturerId))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Car {CarId} ({CarName}) has no manufacturer mapping. Skipping.", carJson.Id, carJson.Name);
                }
                continue;
            }

            var car = new Car
            {
                Id = carJson.Id,
                Name = carJson.Name,
                ClassId = carJson.Class,
                ManufacturerId = manufacturerId
            };

            cars.Add(car);

            // Add liveries for this car
            foreach (var liveryJson in carWithLiveries.Liveries)
            {
                liveries.Add(new Livery
                {
                    Id = liveryJson.Id,
                    Name = liveryJson.Name,
                    IsDefault = liveryJson.IsDefault,
                    CarId = car.Id
                });
            }
        }

        await dbContext.Cars.AddRangeAsync(cars, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} cars", cars.Count);
        }

        await dbContext.Liveries.AddRangeAsync(liveries, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} liveries", liveries.Count);
        }
    }

    private async Task SeedTracksAndLayoutsAsync(ContentRootJson contentData, TrackLocationsRootJson trackLocationsData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeding tracks and layouts...");
        }

        // Create a lookup of TrackId -> Location
        var trackLocations = trackLocationsData.Tracks
            .ToDictionary(t => t.Id, t => t.Location);

        var tracks = new List<Track>();
        var layouts = new List<Layout>();

        foreach (var trackJson in contentData.Tracks)
        {
            // Get location from lookup, default to null if not found
            trackLocations.TryGetValue(trackJson.Id, out var location);

            var track = new Track
            {
                Id = trackJson.Id,
                Name = trackJson.Name,
                Location = location
            };

            tracks.Add(track);

            foreach (var layoutJson in trackJson.Layouts)
            {
                layouts.Add(new Layout
                {
                    Id = layoutJson.Id,
                    Name = layoutJson.Name,
                    MaxVehicles = layoutJson.MaxVehicles,
                    TrackId = track.Id
                });
            }
        }

        await dbContext.Tracks.AddRangeAsync(tracks, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} tracks", tracks.Count);
        }

        await dbContext.Layouts.AddRangeAsync(layouts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Seeded {Count} layouts", layouts.Count);
        }
    }
}
