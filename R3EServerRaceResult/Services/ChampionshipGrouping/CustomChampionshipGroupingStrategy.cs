using R3EServerRaceResult.Models.R3EServerResult;
using System.Text.RegularExpressions;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public partial class CustomChampionshipGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly ILogger<CustomChampionshipGroupingStrategy> logger;
        private readonly ChampionshipConfigurationStore configurationStore;
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public CustomChampionshipGroupingStrategy(
            ILogger<CustomChampionshipGroupingStrategy> logger,
            ChampionshipConfigurationStore configurationStore
            )
        {
            this.logger = logger;
            this.configurationStore = configurationStore;
        }

        public async Task<string> GetChampionshipKeyAsync(Result raceResult)
        {
            var config = await GetOrCreateConfigurationAsync(raceResult);
            return config.Id;
        }

        public async Task<string> GetEventNameAsync(Result raceResult)
        {
            var config = await GetOrCreateConfigurationAsync(raceResult);
            return config.Name;
        }

        public async Task<string> GetSummaryFolderAsync(Result raceResult)
        {
            var config = await GetOrCreateConfigurationAsync(raceResult);

            // Create a safe folder name from championship name
            var safeName = SafeFolderNameRegex().Replace(config.Name, "_");
            return Path.Combine(config.StartDate.Year.ToString(), "custom-championships", $"{safeName}_{config.Id}");
        }

        private async Task<Models.ChampionshipConfiguration> GetOrCreateConfigurationAsync(Result raceResult)
        {
            // Use semaphore to prevent race conditions
            await semaphore.WaitAsync();
            try
            {
                var config = await configurationStore.GetConfigurationForDateAsync(raceResult.StartTime);

                if (config != null)
                {
                    return config;
                }

                // No configuration found - create a single-race championship
                var raceDate = DateOnly.FromDateTime(raceResult.StartTime);
                var newConfig = new Models.ChampionshipConfiguration
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Race {raceResult.StartTime:yyyy-MM-dd HH:mm}",
                    StartDate = raceDate,
                    EndDate = raceDate,
                    CreatedAt = DateTime.UtcNow
                };

                var (success, errorMessage) = await configurationStore.AddConfigurationAsync(newConfig);

                if (!success)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Failed to auto-create championship for race date {RaceDate}: {Error}. Using fallback.",
                            raceResult.StartTime, errorMessage);
                    }

                    // If auto-creation fails (e.g., overlap), try to find the existing one again
                    config = await configurationStore.GetConfigurationForDateAsync(raceResult.StartTime);
                    if (config != null)
                    {
                        return config;
                    }

                    // Final fallback: return a temporary configuration (won't be persisted)
                    return newConfig;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Auto-created single-race championship: {Name} for race date {RaceDate}",
                        newConfig.Name, raceResult.StartTime);
                }

                return newConfig;
            }
            finally
            {
                semaphore.Release();
            }
        }

        [GeneratedRegex(@"[^a-zA-Z0-9_\-]")]
        private static partial Regex SafeFolderNameRegex();
    }
}
