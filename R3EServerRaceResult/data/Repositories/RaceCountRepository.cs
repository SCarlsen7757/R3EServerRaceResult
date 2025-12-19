using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models;

namespace R3EServerRaceResult.Data.Repositories
{
    public class RaceCountRepository : IRaceCountRepository
    {
        private readonly ChampionshipDbContext context;
        private readonly ILogger<RaceCountRepository> logger;

        public RaceCountRepository(
            ChampionshipDbContext context,
            ILogger<RaceCountRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<RaceCountState?> GetByYearAsync(int year)
        {
            return await context.RaceCountStates
                .FirstOrDefaultAsync(r => r.Year == year);
        }

        public async Task<int> IncrementRaceCountAsync(int year, int racesPerChampionship)
        {
            var state = await GetByYearAsync(year);

            if (state == null)
            {
                state = new RaceCountState
                {
                    Year = year,
                    RaceCount = 1,
                    RacesPerChampionship = racesPerChampionship,
                    LastUpdated = DateTime.UtcNow
                };
                context.RaceCountStates.Add(state);
            }
            else
            {
                // Update configuration if it changed
                state.RacesPerChampionship = racesPerChampionship;
                state.RaceCount++;
                state.LastUpdated = DateTime.UtcNow;
            }

            try
            {
                await context.SaveChangesAsync();

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Incremented race count for year {Year} to {RaceCount} (Config: {RacesPerChamp} races/champ)",
                        year, state.RaceCount, racesPerChampionship);
                }

                return state.RaceCount;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error incrementing race count for year {Year}", year);
                }
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetAllRaceCountsAsync()
        {
            var states = await context.RaceCountStates.ToListAsync();
            return states.ToDictionary(s => s.Year, s => s.RaceCount);
        }

        public async Task<bool> ValidateConfigurationAsync(int year, int racesPerChampionship)
        {
            var state = await GetByYearAsync(year);

            if (state == null)
            {
                // No existing state - configuration is valid
                return true;
            }

            // Check if configuration changed
            bool configChanged = state.RacesPerChampionship != racesPerChampionship;

            if (configChanged && state.RaceCount > 0)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(
                        "Configuration change detected for year {Year}: " +
                        "RacesPerChampionship {OldRaces}?{NewRaces}. " +
                        "Existing race count: {RaceCount}",
                        year,
                        state.RacesPerChampionship, racesPerChampionship,
                        state.RaceCount);
                }
                return false;
            }

            return true;
        }

        public async Task ResetCountForYearAsync(int year, int racesPerChampionship, string? reason = null)
        {
            var state = await GetByYearAsync(year);

            if (state == null)
            {
                // Create new state with count 0
                state = new RaceCountState
                {
                    Year = year,
                    RaceCount = 0,
                    RacesPerChampionship = racesPerChampionship,
                    LastUpdated = DateTime.UtcNow
                };
                context.RaceCountStates.Add(state);
            }
            else
            {
                // Reset existing state
                var oldCount = state.RaceCount;
                state.RaceCount = 0;
                state.RacesPerChampionship = racesPerChampionship;
                state.LastUpdated = DateTime.UtcNow;

                if (logger.IsEnabled(LogLevel.Warning))
                {
                    var reasonMessage = !string.IsNullOrEmpty(reason) ? $" Reason: {reason}" : "";
                    logger.LogWarning(
                        "Reset race count for year {Year} from {OldCount} to 0. " +
                        "New config: {RacesPerChamp} races/championship.{Reason}",
                        year, oldCount, racesPerChampionship, reasonMessage);
                }
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error resetting race count for year {Year}", year);
                }
                throw;
            }
        }
    }
}
