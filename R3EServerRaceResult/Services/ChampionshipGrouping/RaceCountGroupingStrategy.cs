using R3EServerRaceResult.Data.Repositories;
using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class RaceCountGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly ILogger<RaceCountGroupingStrategy> logger;
        private readonly int racesPerChampionship;
        private readonly IRaceCountRepository repository;
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly Dictionary<int, int> raceCountCache = [];
        private readonly Lazy<Task> initializationTask;

        public RaceCountGroupingStrategy(
            ILogger<RaceCountGroupingStrategy> logger,
            int racesPerChampionship,
            IRaceCountRepository repository)
        {
            if (racesPerChampionship <= 0)
            {
                throw new ArgumentException("Races per championship must be greater than 0", nameof(racesPerChampionship));
            }

            this.racesPerChampionship = racesPerChampionship;
            this.repository = repository;
            this.logger = logger;

            initializationTask = new Lazy<Task>(async () =>
            {
                await LoadCacheAsync();
                await ValidateConfigurationAsync();
            });
        }

        public async Task<string> GetChampionshipKeyAsync(Result raceResult)
        {
            await EnsureInitializedAsync();
            var championshipNumber = await GetChampionshipNumberAsync(raceResult.StartTime);
            var year = raceResult.StartTime.Year;
            return $"{year}-C{championshipNumber:D2}";
        }

        public async Task<string> GetEventNameAsync(Result raceResult)
        {
            await EnsureInitializedAsync();
            var year = raceResult.StartTime.Year;
            var championshipNumber = await GetChampionshipNumberAsync(raceResult.StartTime);

            await semaphore.WaitAsync();
            try
            {
                var raceNumber = await GetRaceNumberAsync(year);
                await IncrementRaceCountAsync(year);
                return $"Championship {championshipNumber} - Race {raceNumber} ({year})";
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<string> GetSummaryFolderAsync(Result raceResult)
        {
            await EnsureInitializedAsync();
            var year = raceResult.StartTime.Year;
            var championshipNumber = await GetChampionshipNumberAsync(raceResult.StartTime);
            return Path.Combine(year.ToString(), $"champ{championshipNumber}");
        }

        private async Task EnsureInitializedAsync()
        {
            await initializationTask.Value;
        }

        private async Task<int> GetChampionshipNumberAsync(DateTime raceDate)
        {
            var raceYear = raceDate.Year;
            var count = await GetCachedRaceCountAsync(raceYear);
            return (count / racesPerChampionship) + 1;
        }

        private async Task<int> GetRaceNumberAsync(int year)
        {
            var count = await GetCachedRaceCountAsync(year);
            return (count % racesPerChampionship) + 1;
        }

        private async Task IncrementRaceCountAsync(int year)
        {
            try
            {
                var newCount = await repository.IncrementRaceCountAsync(year, racesPerChampionship);
                raceCountCache[year] = newCount;

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Race count incremented for year {Year}: {Count}", year, newCount);
                }
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

        private async Task<int> GetCachedRaceCountAsync(int year)
        {
            if (raceCountCache.TryGetValue(year, out var count))
            {
                return count;
            }

            var state = await repository.GetByYearAsync(year);
            count = state?.RaceCount ?? 0;
            raceCountCache[year] = count;
            return count;
        }

        private async Task LoadCacheAsync()
        {
            try
            {
                var allCounts = await repository.GetAllRaceCountsAsync();
                foreach (var kvp in allCounts)
                {
                    raceCountCache[kvp.Key] = kvp.Value;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Loaded {Count} race count states from database", allCounts.Count);
                }
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error loading race count cache from database");
                }
            }
        }

        private async Task ValidateConfigurationAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            // Check current year and next year (in case races span year boundary)
            for (int year = currentYear; year <= currentYear + 1; year++)
            {
                var isValid = await repository.ValidateConfigurationAsync(year, racesPerChampionship);

                if (!isValid)
                {
                    var state = await repository.GetByYearAsync(year);

                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning(
                            "Configuration mismatch detected for year {Year}. " +
                            "Database: {OldRaces} races/championship. " +
                            "Current: {NewRaces} races/championship. " +
                            "Race count will be reset to 0 to start fresh with new configuration.",
                            year,
                            state?.RacesPerChampionship,
                            racesPerChampionship);
                    }

                    // Reset counter for this year due to configuration change
                    await repository.ResetCountForYearAsync(year, racesPerChampionship, "Configuration change detected on startup");
                    raceCountCache[year] = 0;
                }
            }
        }
    }
}
