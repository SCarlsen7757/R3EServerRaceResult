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
        this.contentJsonPath = Path.Combine(basePath, "Content.json");
        this.manufacturersJsonPath = Path.Combine(basePath, "Manufacturers.json");
        this.tracksJsonPath = Path.Combine(basePath, "Tracks.json");
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Starting R3E content data sync from JSON files...");
            }

            // Parse JSON files
            var contentData = await ParseContentJsonAsync(cancellationToken);
            var manufacturersData = await ParseManufacturersJsonAsync(cancellationToken);
            var trackLocationsData = await ParseTrackLocationsJsonAsync(cancellationToken);

            // Upsert data (update existing or insert new)
            await UpsertManufacturersAsync(manufacturersData, cancellationToken);
            await UpsertCarClassesAsync(contentData, cancellationToken);
            await UpsertCarsAndLiveriesAsync(contentData, manufacturersData, cancellationToken);
            await UpsertTracksAndLayoutsAsync(contentData, trackLocationsData, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("R3E content data sync completed successfully");
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error syncing R3E content data");
            }
            throw;
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

    private async Task UpsertManufacturersAsync(ManufacturersRootJson manufacturersData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Syncing {Count} manufacturers...", manufacturersData.Manufacturers.Count);
        }

        var added = 0;
        var updated = 0;

        foreach (var manufacturerJson in manufacturersData.Manufacturers)
        {
            var existing = await dbContext.Manufacturers
                .FirstOrDefaultAsync(m => m.Id == manufacturerJson.Id, cancellationToken);

            if (existing == null)
            {
                dbContext.Manufacturers.Add(new Manufacturer
                {
                    Id = manufacturerJson.Id,
                    Name = manufacturerJson.Name,
                    CountryCode = manufacturerJson.Country
                });
                added++;
            }
            else
            {
                var changed = false;
                if (existing.Name != manufacturerJson.Name)
                {
                    existing.Name = manufacturerJson.Name;
                    changed = true;
                }
                if (existing.CountryCode != manufacturerJson.Country)
                {
                    existing.CountryCode = manufacturerJson.Country;
                    changed = true;
                }
                if (changed) updated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Manufacturers: {Added} added, {Updated} updated", added, updated);
        }
    }

    private async Task UpsertCarClassesAsync(ContentRootJson contentData, CancellationToken cancellationToken)
    {
        var uniqueClassIds = contentData.Liveries
            .Select(l => l.Car.Class)
            .Distinct()
            .ToList();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Syncing {Count} car classes...", uniqueClassIds.Count);
        }

        var added = 0;

        foreach (var classId in uniqueClassIds)
        {
            var existing = await dbContext.CarClasses
                .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);

            if (existing == null)
            {
                dbContext.CarClasses.Add(new CarClass
                {
                    Id = classId,
                    Name = $"Class {classId}"
                });
                added++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Car classes: {Added} added", added);
        }
    }

    private async Task UpsertCarsAndLiveriesAsync(ContentRootJson contentData, ManufacturersRootJson manufacturersData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Syncing cars and liveries...");
        }

        // Create a lookup of CarId -> ManufacturerId
        var carToManufacturer = manufacturersData.Manufacturers
            .SelectMany(m => m.CarIds.Select(carId => new { CarId = carId, ManufacturerId = m.Id }))
            .ToDictionary(x => x.CarId, x => x.ManufacturerId);

        var carsAdded = 0;
        var carsUpdated = 0;
        var liveriesAdded = 0;
        var liveriesUpdated = 0;

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

            // Upsert car
            var existingCar = await dbContext.Cars
                .FirstOrDefaultAsync(c => c.Id == carJson.Id, cancellationToken);

            if (existingCar == null)
            {
                existingCar = new Car
                {
                    Id = carJson.Id,
                    Name = carJson.Name,
                    ClassId = carJson.Class,
                    ManufacturerId = manufacturerId
                };
                dbContext.Cars.Add(existingCar);
                carsAdded++;
            }
            else
            {
                var changed = false;
                if (existingCar.Name != carJson.Name)
                {
                    existingCar.Name = carJson.Name;
                    changed = true;
                }
                if (existingCar.ClassId != carJson.Class)
                {
                    existingCar.ClassId = carJson.Class;
                    changed = true;
                }
                if (existingCar.ManufacturerId != manufacturerId)
                {
                    existingCar.ManufacturerId = manufacturerId;
                    changed = true;
                }
                if (changed) carsUpdated++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Upsert liveries for this car
            foreach (var liveryJson in carWithLiveries.Liveries)
            {
                var existingLivery = await dbContext.Liveries
                    .FirstOrDefaultAsync(l => l.Id == liveryJson.Id, cancellationToken);

                if (existingLivery == null)
                {
                    dbContext.Liveries.Add(new Livery
                    {
                        Id = liveryJson.Id,
                        Name = liveryJson.Name,
                        IsDefault = liveryJson.IsDefault,
                        CarId = existingCar.Id
                    });
                    liveriesAdded++;
                }
                else
                {
                    var changed = false;
                    if (existingLivery.Name != liveryJson.Name)
                    {
                        existingLivery.Name = liveryJson.Name;
                        changed = true;
                    }
                    if (existingLivery.IsDefault != liveryJson.IsDefault)
                    {
                        existingLivery.IsDefault = liveryJson.IsDefault;
                        changed = true;
                    }
                    if (existingLivery.CarId != existingCar.Id)
                    {
                        existingLivery.CarId = existingCar.Id;
                        changed = true;
                    }
                    if (changed) liveriesUpdated++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Cars: {Added} added, {Updated} updated", carsAdded, carsUpdated);
            logger.LogInformation("Liveries: {Added} added, {Updated} updated", liveriesAdded, liveriesUpdated);
        }
    }

    private async Task UpsertTracksAndLayoutsAsync(ContentRootJson contentData, TrackLocationsRootJson trackLocationsData, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Syncing tracks and layouts...");
        }

        // Create a lookup of TrackId -> Location
        var trackLocations = trackLocationsData.Tracks
            .ToDictionary(t => t.Id, t => t.Location);

        var tracksAdded = 0;
        var tracksUpdated = 0;
        var layoutsAdded = 0;
        var layoutsUpdated = 0;

        foreach (var trackJson in contentData.Tracks)
        {
            // Get location from lookup
            trackLocations.TryGetValue(trackJson.Id, out var location);

            // Upsert track
            var existingTrack = await dbContext.Tracks
                .FirstOrDefaultAsync(t => t.Id == trackJson.Id, cancellationToken);

            if (existingTrack == null)
            {
                existingTrack = new Track
                {
                    Id = trackJson.Id,
                    Name = trackJson.Name,
                    CountryCode = location
                };
                dbContext.Tracks.Add(existingTrack);
                tracksAdded++;
            }
            else
            {
                var changed = false;
                if (existingTrack.Name != trackJson.Name)
                {
                    existingTrack.Name = trackJson.Name;
                    changed = true;
                }
                if (existingTrack.CountryCode != location)
                {
                    existingTrack.CountryCode = location;
                    changed = true;
                }
                if (changed) tracksUpdated++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Upsert layouts for this track
            foreach (var layoutJson in trackJson.Layouts)
            {
                var existingLayout = await dbContext.Layouts
                    .FirstOrDefaultAsync(l => l.Id == layoutJson.Id, cancellationToken);

                if (existingLayout == null)
                {
                    dbContext.Layouts.Add(new Layout
                    {
                        Id = layoutJson.Id,
                        Name = layoutJson.Name,
                        MaxVehicles = layoutJson.MaxVehicles,
                        TrackId = existingTrack.Id
                    });
                    layoutsAdded++;
                }
                else
                {
                    var changed = false;
                    if (existingLayout.Name != layoutJson.Name)
                    {
                        existingLayout.Name = layoutJson.Name;
                        changed = true;
                    }
                    if (existingLayout.MaxVehicles != layoutJson.MaxVehicles)
                    {
                        existingLayout.MaxVehicles = layoutJson.MaxVehicles;
                        changed = true;
                    }
                    if (existingLayout.TrackId != existingTrack.Id)
                    {
                        existingLayout.TrackId = existingTrack.Id;
                        changed = true;
                    }
                    if (changed) layoutsUpdated++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Tracks: {Added} added, {Updated} updated", tracksAdded, tracksUpdated);
            logger.LogInformation("Layouts: {Added} added, {Updated} updated", layoutsAdded, layoutsUpdated);
        }
    }
}
