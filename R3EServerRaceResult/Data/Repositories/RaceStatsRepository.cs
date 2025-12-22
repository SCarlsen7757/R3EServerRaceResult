using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models.RaceStats;

namespace R3EServerRaceResult.Data.Repositories;

public class RaceStatsRepository : IRaceStatsRepository
{
    private readonly RaceStatsDbContext dbContext;
    private readonly R3EContentDbContext contentDbContext;

    public RaceStatsRepository(RaceStatsDbContext dbContext, R3EContentDbContext contentDbContext)
    {
        this.dbContext = dbContext;
        this.contentDbContext = contentDbContext;
    }

    public async Task<Driver?> GetDriverByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Drivers.FindAsync([userId], cancellationToken);
    }

    public async Task<Driver> GetOrCreateDriverAsync(int userId, string fullName, CancellationToken cancellationToken = default)
    {
        var existingDriver = await dbContext.Drivers.FindAsync([userId], cancellationToken);
        
        if (existingDriver != null)
        {
            return existingDriver;
        }

        var shortenedName = NameShortener.ShortenName(fullName);
        var newDriver = new Driver
        {
            Id = userId,
            Name = shortenedName
        };

        dbContext.Drivers.Add(newDriver);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return newDriver;
    }

    public async Task<Event> CreateEventAsync(Event raceEvent, CancellationToken cancellationToken = default)
    {
        dbContext.Events.Add(raceEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return raceEvent;
    }

    public async Task<Event?> GetEventByDateAndTrackAsync(DateTime eventDate, int trackId, int layoutId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .FirstOrDefaultAsync(e => 
                e.EventDate.Date == eventDate.Date && 
                e.TrackId == trackId && 
                e.LayoutId == layoutId, 
                cancellationToken);
    }

    public async Task<RaceSession> CreateSessionAsync(RaceSession session, CancellationToken cancellationToken = default)
    {
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<RaceSession?> GetSessionByEventAndTypeAsync(int eventId, SessionType sessionType, int sessionNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Sessions
            .FirstOrDefaultAsync(s => 
                s.EventId == eventId && 
                s.SessionType == sessionType && 
                s.SessionNumber == sessionNumber, 
                cancellationToken);
    }

    public async Task DeleteSessionAsync(RaceSession session, CancellationToken cancellationToken = default)
    {
        dbContext.Sessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Event?> GetEventByDetailsAsync(DateTime eventDate, int trackId, int layoutId, string serverName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .FirstOrDefaultAsync(e => 
                e.EventDate.Date == eventDate.Date && 
                e.TrackId == trackId && 
                e.LayoutId == layoutId &&
                e.ServerName == serverName, 
                cancellationToken);
    }

    public async Task DeleteEventIfEmptyAsync(int eventId, CancellationToken cancellationToken = default)
    {
        // Check if event has any remaining sessions
        var hasRemainingSessions = await dbContext.Sessions
            .AnyAsync(s => s.EventId == eventId, cancellationToken);

        if (!hasRemainingSessions)
        {
            var eventToDelete = await dbContext.Events.FindAsync([eventId], cancellationToken);
            if (eventToDelete != null)
            {
                dbContext.Events.Remove(eventToDelete);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<RaceResult> CreateResultAsync(RaceResult result, CancellationToken cancellationToken = default)
    {
        dbContext.Results.Add(result);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task CreateResultsAsync(IEnumerable<RaceResult> results, CancellationToken cancellationToken = default)
    {
        dbContext.Results.AddRange(results);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateLapsAsync(IEnumerable<Lap> laps, CancellationToken cancellationToken = default)
    {
        dbContext.Laps.AddRange(laps);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateIncidentsAsync(IEnumerable<RaceIncident> incidents, CancellationToken cancellationToken = default)
    {
        dbContext.Incidents.AddRange(incidents);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<(int CarId, int UsageCount)>> GetMostUsedCarsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var carUsage = await dbContext.Results
            .GroupBy(r => r.CarId)
            .Select(g => new { CarId = g.Key, UsageCount = g.Count() })
            .OrderByDescending(x => x.UsageCount)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return carUsage.Select(x => (x.CarId, x.UsageCount));
    }

    public async Task<IEnumerable<(int ManufacturerId, int UsageCount)>> GetMostUsedManufacturersAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        // Get car usage from race stats
        var carUsage = await dbContext.Results
            .GroupBy(r => r.CarId)
            .Select(g => new { CarId = g.Key, UsageCount = g.Count() })
            .ToListAsync(cancellationToken);

        // Get manufacturer mapping from R3E content database
        var carIds = carUsage.Select(x => x.CarId).ToList();
        var carManufacturers = await contentDbContext.Cars
            .Where(c => carIds.Contains(c.Id))
            .Select(c => new { c.Id, c.ManufacturerId })
            .ToListAsync(cancellationToken);

        // Combine results
        var manufacturerUsage = carUsage
            .Join(carManufacturers, 
                cu => cu.CarId, 
                cm => cm.Id, 
                (cu, cm) => new { cm.ManufacturerId, cu.UsageCount })
            .GroupBy(x => x.ManufacturerId)
            .Select(g => (ManufacturerId: g.Key, UsageCount: g.Sum(x => x.UsageCount)))
            .OrderByDescending(x => x.UsageCount)
            .Take(limit);

        return manufacturerUsage;
    }
}
