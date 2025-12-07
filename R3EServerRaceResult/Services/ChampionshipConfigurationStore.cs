using System.Text.Json;

namespace R3EServerRaceResult.Services
{
    public class ChampionshipConfigurationStore
    {
        private readonly string stateFilePath;
        private readonly List<Models.ChampionshipConfiguration> configurations = [];
        private readonly Lock @lock = new();
        private readonly ILogger<ChampionshipConfigurationStore> logger;

        public ChampionshipConfigurationStore(string mountedVolumePath, ILogger<ChampionshipConfigurationStore> logger)
        {
            stateFilePath = Path.Combine(mountedVolumePath, ".championship_configs.json");
            this.logger = logger;
            LoadConfigurations();
        }

        public (bool success, string? errorMessage) AddConfiguration(Models.ChampionshipConfiguration config)
        {
            lock (@lock)
            {
                var (isValid, validationError) = config.Validate();
                if (!isValid)
                {
                    return (false, validationError);
                }

                // Check for overlaps with existing configurations
                foreach (var existingConfig in configurations.Where(c => c.IsActive))
                {
                    if (config.OverlapsWith(existingConfig))
                    {
                        return (false, $"Championship period overlaps with existing championship '{existingConfig.Name}' ({existingConfig.StartDate} to {existingConfig.EndDate})");
                    }
                }

                configurations.Add(config);
                SaveConfigurations();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration added: {Name} ({StartDate} to {EndDate})",
                        config.Name, config.StartDate, config.EndDate);
                }

                return (true, null);
            }
        }

        public Models.ChampionshipConfiguration? GetConfigurationById(string id)
        {
            lock (@lock)
            {
                return configurations.FirstOrDefault(c => c.Id == id);
            }
        }

        public Models.ChampionshipConfiguration? GetConfigurationForDate(DateTime date)
        {
            lock (@lock)
            {
                return configurations
                    .Where(c => c.IsActive && c.ContainsDate(date))
                    .OrderBy(c => c.StartDate)
                    .FirstOrDefault();
            }
        }

        public List<Models.ChampionshipConfiguration> GetAllConfigurations(bool includeExpired = true)
        {
            lock (@lock)
            {
                if (includeExpired)
                {
                    return [.. configurations];
                }

                return configurations.Where(c => !c.IsExpired).ToList();
            }
        }

        public (bool success, string? errorMessage) UpdateConfiguration(string id, Models.ChampionshipConfiguration updatedConfig)
        {
            lock (@lock)
            {
                var existing = configurations.FirstOrDefault(c => c.Id == id);
                if (existing == null)
                {
                    return (false, "Championship configuration not found");
                }

                var (isValid, validationError) = updatedConfig.Validate();
                if (!isValid)
                {
                    return (false, validationError);
                }

                // Check for overlaps with other configurations (excluding self)
                foreach (var config in configurations.Where(c => c.IsActive && c.Id != id))
                {
                    if (updatedConfig.OverlapsWith(config))
                    {
                        return (false, $"Championship period overlaps with existing championship '{config.Name}' ({config.StartDate} to {config.EndDate})");
                    }
                }

                existing.Name = updatedConfig.Name;
                existing.StartDate = updatedConfig.StartDate;
                existing.EndDate = updatedConfig.EndDate;
                existing.IsActive = updatedConfig.IsActive;

                SaveConfigurations();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration updated: {Id} - {Name}", id, updatedConfig.Name);
                }

                return (true, null);
            }
        }

        public bool RemoveConfiguration(string id)
        {
            lock (@lock)
            {
                var config = configurations.FirstOrDefault(c => c.Id == id);
                if (config == null)
                {
                    return false;
                }

                configurations.Remove(config);
                SaveConfigurations();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration removed: {Id} - {Name}", id, config.Name);
                }

                return true;
            }
        }

        private void LoadConfigurations()
        {
            lock (@lock)
            {
                try
                {
                    if (File.Exists(stateFilePath))
                    {
                        var json = File.ReadAllText(stateFilePath);
                        var configs = JsonSerializer.Deserialize<List<Models.ChampionshipConfiguration>>(json);
                        if (configs != null)
                        {
                            configurations.Clear();
                            configurations.AddRange(configs);

                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation("Loaded {Count} championship configurations from disk", configs.Count);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Error loading championship configurations, starting fresh");
                    }
                    configurations.Clear();
                }
            }
        }

        private void SaveConfigurations()
        {
            try
            {
                var tempPath = stateFilePath + ".tmp";
                var json = JsonSerializer.Serialize(configurations, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);

                if (File.Exists(stateFilePath))
                {
                    File.Delete(stateFilePath);
                }

                File.Move(tempPath, stateFilePath);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error saving championship configurations");
                }
            }
        }
    }
}
