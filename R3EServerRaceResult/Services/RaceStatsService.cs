using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Data;
using R3EServerRaceResult.Data.Repositories;
using R3EServerRaceResult.Models.R3EServerResult;
using R3EServerRaceResult.Models.RaceStats;

namespace R3EServerRaceResult.Services;

public class RaceStatsService
{
    private readonly IRaceStatsRepository raceStatsRepository;
    private readonly R3EContentDbContext contentDbContext;
    private readonly ILogger<RaceStatsService> logger;

    public RaceStatsService(
        IRaceStatsRepository raceStatsRepository,
        R3EContentDbContext contentDbContext,
        ILogger<RaceStatsService> logger)
    {
        this.raceStatsRepository = raceStatsRepository;
        this.contentDbContext = contentDbContext;
        this.logger = logger;
    }

    /// <summary>
    /// Process a race result and insert race sessions into the database
    /// Only processes Race sessions (not Practice or Qualify)
    /// </summary>
    public async Task ProcessRaceResultAsync(Result result, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find track and layout from R3E content database
            var (trackId, layoutId) = await FindTrackAndLayoutAsync(result.Track, result.TrackLayout, cancellationToken);

            if (trackId == 0 || layoutId == 0)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Track or layout not found for '{Track}' - '{Layout}'. Skipping race stats.",
                        result.Track, result.TrackLayout);
                }
                return;
            }

            // Create or get existing event
            var raceEvent = await raceStatsRepository.GetEventByDateAndTrackAsync(
                result.StartTime, trackId, layoutId, cancellationToken);

            if (raceEvent == null)
            {
                raceEvent = await raceStatsRepository.CreateEventAsync(new Event
                {
                    TrackId = trackId,
                    LayoutId = layoutId,
                    EventDate = result.StartTime,
                    ServerName = result.Server
                }, cancellationToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Created new event for {Track} on {Date}", result.Track, result.StartTime);
                }
            }

            // Process only Race sessions
            var raceSessions = result.Sessions
                .Where(s => IsRaceSession(s.Type))
                .ToList();

            if (raceSessions.Count == 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("No race sessions found in result. Skipping race stats.");
                }
                return;
            }

            foreach (var session in raceSessions)
            {
                await ProcessSessionAsync(raceEvent, session, cancellationToken);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Processed {Count} race sessions for event at {Track}",
                    raceSessions.Count, result.Track);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error processing race result for race stats");
            }
            throw;
        }
    }

    /// <summary>
    /// Delete race stats for a specific race result
    /// </summary>
    public async Task DeleteRaceResultAsync(Result result, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find track and layout
            var (trackId, layoutId) = await FindTrackAndLayoutAsync(result.Track, result.TrackLayout, cancellationToken);
            
            if (trackId == 0 || layoutId == 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Track or layout not found for deletion. Skipping race stats cleanup.");
                }
                return;
            }

            // Find the event
            var raceEvent = await raceStatsRepository.GetEventByDetailsAsync(
                result.StartTime, trackId, layoutId, result.Server, cancellationToken);

            if (raceEvent == null)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Event not found for deletion. Race stats may not have been recorded.");
                }
                return;
            }

            // Delete all race sessions for this event
            var raceSessions = result.Sessions
                .Where(s => IsRaceSession(s.Type))
                .ToList();

            foreach (var session in raceSessions)
            {
                var sessionNumber = GetRaceSessionNumber(session.Type);
                var existingSession = await raceStatsRepository.GetSessionByEventAndTypeAsync(
                    raceEvent.Id, SessionType.Race, sessionNumber, cancellationToken);

                if (existingSession != null)
                {
                    // Cascade delete will handle Results, Laps, and Incidents
                    await raceStatsRepository.DeleteSessionAsync(existingSession, cancellationToken);
                    
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Deleted race session {SessionType} #{Number} for event {EventId}",
                            SessionType.Race, sessionNumber, raceEvent.Id);
                    }
                }
            }

            // Delete event if no sessions remain
            await raceStatsRepository.DeleteEventIfEmptyAsync(raceEvent.Id, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Race stats cleanup completed for {Track} on {Date}", 
                    result.Track, result.StartTime);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Error deleting race stats");
            }
            throw;
        }
    }

    private async Task ProcessSessionAsync(Event raceEvent, Session session, CancellationToken cancellationToken)
    {
        var sessionNumber = GetRaceSessionNumber(session.Type);
        
        // Check if session already exists to prevent duplicates
        var existingSession = await raceStatsRepository.GetSessionByEventAndTypeAsync(
            raceEvent.Id, SessionType.Race, sessionNumber, cancellationToken);

        if (existingSession != null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Session {SessionType} #{Number} for event {EventId} already exists. Skipping duplicate.",
                    SessionType.Race, sessionNumber, raceEvent.Id);
            }
            return; // Skip processing duplicate session
        }

        // Create session
        var raceSession = await raceStatsRepository.CreateSessionAsync(new RaceSession
        {
            EventId = raceEvent.Id,
            SessionType = SessionType.Race,
            SessionNumber = sessionNumber
        }, cancellationToken);

        var results = new List<RaceResult>();
        var allLaps = new List<Lap>();
        var allIncidents = new List<RaceIncident>();

        foreach (var player in session.Players)
        {
            // Get or create driver
            var driver = await raceStatsRepository.GetOrCreateDriverAsync(
                player.UserId, player.FullName, cancellationToken);

            // Create result
            var raceResult = new RaceResult
            {
                DriverId = driver.Id,
                SessionId = raceSession.Id,
                CarId = player.CarId,
                StartPosition = player.StartPosition,
                Position = player.Position,
                ClassStartPosition = player.StartPositionInClass,
                ClassPosition = player.PositionInClass,
                TotalRaceTime = (long)player.TotalTime.TotalMilliseconds,
                BestLapTime = player.BestLapTime.TotalMilliseconds < 0 
                    ? null 
                    : (long)player.BestLapTime.TotalMilliseconds,
                FinishStatus = player.FinishStatus,
                TotalLaps = player.RaceSessionLaps.Count
            };
            results.Add(raceResult);

            // Process laps
            for (int lapIndex = 0; lapIndex < player.RaceSessionLaps.Count; lapIndex++)
            {
                var lap = player.RaceSessionLaps[lapIndex];
                
                // Convert negative times to null (invalid/incomplete)
                long? lapTime = lap.Time.TotalMilliseconds < 0 ? null : (long)lap.Time.TotalMilliseconds;
                long? sector1 = lap.SectorTimes.Count > 0 && lap.SectorTimes[0].TotalMilliseconds >= 0 
                    ? (long)lap.SectorTimes[0].TotalMilliseconds 
                    : null;
                long? sector2 = lap.SectorTimes.Count > 1 && lap.SectorTimes[1].TotalMilliseconds >= 0 
                    ? (long)lap.SectorTimes[1].TotalMilliseconds 
                    : null;
                long? sector3 = lap.SectorTimes.Count > 2 && lap.SectorTimes[2].TotalMilliseconds >= 0 
                    ? (long)lap.SectorTimes[2].TotalMilliseconds 
                    : null;
                
                allLaps.Add(new Lap
                {
                    DriverId = driver.Id,
                    SessionId = raceSession.Id,
                    LapNumber = lapIndex + 1,
                    LapTime = lapTime,
                    Sector1Time = sector1,
                    Sector2Time = sector2,
                    Sector3Time = sector3,
                    IsValid = lap.Valid
                });

                // Process incidents for this lap
                foreach (var incident in lap.Incidents)
                {
                    allIncidents.Add(new RaceIncident
                    {
                        DriverId = driver.Id,
                        SessionId = raceSession.Id,
                        IncidentType = incident.Type,
                        IncidentPoints = incident.Points,
                        LapNumber = lapIndex + 1,
                        InvolvedDriverId = incident.OtherUserId > 0 ? incident.OtherUserId : null
                    });
                }
            }
        }

        // Save all results, laps, and incidents
        await raceStatsRepository.CreateResultsAsync(results, cancellationToken);
        await raceStatsRepository.CreateLapsAsync(allLaps, cancellationToken);
        await raceStatsRepository.CreateIncidentsAsync(allIncidents, cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Created session {SessionType} #{Number} with {Results} results, {Laps} laps, {Incidents} incidents",
                SessionType.Race, sessionNumber, results.Count, allLaps.Count, allIncidents.Count);
        }
    }

    private async Task<(int trackId, int layoutId)> FindTrackAndLayoutAsync(
        string trackName, string layoutName, CancellationToken cancellationToken)
    {
        // Find track by name
        var track = await contentDbContext.Tracks
            .FirstOrDefaultAsync(t => t.Name == trackName, cancellationToken);

        if (track == null)
        {
            return (0, 0);
        }

        // Find layout by name
        var layout = await contentDbContext.Layouts
            .FirstOrDefaultAsync(l => l.TrackId == track.Id && l.Name == layoutName, cancellationToken);

        if (layout == null)
        {
            return (track.Id, 0);
        }

        return (track.Id, layout.Id);
    }

    private static bool IsRaceSession(string sessionType)
    {
        return sessionType.StartsWith("Race", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRaceSessionNumber(string sessionType)
    {
        // Session types: "Race", "Race2", "Race3"
        if (sessionType.Equals("Race", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (sessionType.Length > 4 && int.TryParse(sessionType[4..], out var number))
        {
            return number;
        }

        return 1;
    }
}
