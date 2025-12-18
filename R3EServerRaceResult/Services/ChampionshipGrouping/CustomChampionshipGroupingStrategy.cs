using R3EServerRaceResult.Models.R3EServerResult;
using System.Text.RegularExpressions;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public partial class CustomChampionshipGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly ChampionshipConfigurationStore configurationStore;
        private readonly ILogger<CustomChampionshipGroupingStrategy> logger;

        public CustomChampionshipGroupingStrategy(
            ChampionshipConfigurationStore configurationStore,
            ILogger<CustomChampionshipGroupingStrategy> logger)
        {
            this.configurationStore = configurationStore;
            this.logger = logger;
        }

        public string GetChampionshipKey(Result raceResult)
        {
            var config = GetOrCreateConfiguration(raceResult);
            return config.Id;
        }

        public string GetEventName(Result raceResult)
        {
            var config = GetOrCreateConfiguration(raceResult);
            return config.Name;
        }

        public string GetSummaryFolder(Result raceResult)
        {
            var config = GetOrCreateConfiguration(raceResult);

            // Create a safe folder name from championship name
            var safeName = SafeFolderNameRegex().Replace(config.Name, "_");
            return Path.Combine(config.StartDate.Year.ToString(), "custom-championships", $"{safeName}_{config.Id}");
        }

        private static readonly Lock configurationLock = new();

        private Models.ChampionshipConfiguration GetOrCreateConfiguration(Result raceResult)
        {
            // All logic inside a lock to prevent TOCTOU race condition
            lock (configurationLock)
            {
                var config = configurationStore.GetConfigurationForDate(raceResult.StartTime);

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
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var (success, errorMessage) = configurationStore.AddConfiguration(newConfig);

                if (!success)
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Failed to auto-create championship for race date {RaceDate}: {Error}. Using fallback.",
                            raceResult.StartTime, errorMessage);
                    }

                    // If auto-creation fails (e.g., overlap), try to find the existing one again
                    config = configurationStore.GetConfigurationForDate(raceResult.StartTime);
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
        }

        [GeneratedRegex(@"[^a-zA-Z0-9_\-]")]
        private static partial Regex SafeFolderNameRegex();
    }
}
