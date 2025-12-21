using R3EServerRaceResult.Models.RaceStats;

namespace R3EServerRaceResult.Data.Repositories;

public interface IRaceStatsRepository
{
    // Driver operations
    Task<Driver?> GetDriverByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Driver> GetOrCreateDriverAsync(int userId, string fullName, CancellationToken cancellationToken = default);
    
    // Event operations
    Task<Event> CreateEventAsync(Event raceEvent, CancellationToken cancellationToken = default);
    Task<Event?> GetEventByDateAndTrackAsync(DateTime eventDate, int trackId, int layoutId, CancellationToken cancellationToken = default);
    
    // Session operations
    Task<RaceSession> CreateSessionAsync(RaceSession session, CancellationToken cancellationToken = default);
    
    // Result operations
    Task<RaceResult> CreateResultAsync(RaceResult result, CancellationToken cancellationToken = default);
    Task CreateResultsAsync(IEnumerable<RaceResult> results, CancellationToken cancellationToken = default);
    
    // Lap operations
    Task CreateLapsAsync(IEnumerable<Lap> laps, CancellationToken cancellationToken = default);
    
    // Incident operations
    Task CreateIncidentsAsync(IEnumerable<RaceIncident> incidents, CancellationToken cancellationToken = default);
    
    // Statistics queries
    Task<IEnumerable<(int CarId, int UsageCount)>> GetMostUsedCarsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<(int ManufacturerId, int UsageCount)>> GetMostUsedManufacturersAsync(int limit = 10, CancellationToken cancellationToken = default);
}
