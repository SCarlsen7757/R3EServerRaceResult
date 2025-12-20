using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models;

namespace R3EServerRaceResult.Data.Repositories
{
    public class ChampionshipRepository : IChampionshipRepository
    {
        private readonly ILogger<ChampionshipRepository> logger;
        private readonly ChampionshipDbContext context;

        public ChampionshipRepository(
            ILogger<ChampionshipRepository> logger,
            ChampionshipDbContext context
            )
        {
            this.logger = logger;
            this.context = context;
        }

        public async Task<ChampionshipConfiguration?> GetByIdAsync(string id)
        {
            return await context.ChampionshipConfigurations
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ChampionshipConfiguration?> GetConfigurationForDateAsync(DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);

            return await context.ChampionshipConfigurations
                .Where(c => c.StartDate <= dateOnly && c.EndDate >= dateOnly)
                .OrderBy(c => c.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChampionshipConfiguration>> GetAllAsync(bool includeExpired = true)
        {
            var query = context.ChampionshipConfigurations.AsQueryable();

            if (!includeExpired)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                query = query.Where(c => c.EndDate >= today);
            }

            return await query.ToListAsync();
        }

        public async Task<(bool success, string? errorMessage)> AddAsync(ChampionshipConfiguration config)
        {
            var (isValid, validationError) = config.Validate();
            if (!isValid)
            {
                return (false, validationError);
            }

            if (await HasOverlappingConfigurationsAsync(config))
            {
                var overlapping = await GetOverlappingConfigurationAsync(config);
                return (false, $"Championship period overlaps with existing championship '{overlapping?.Name}' ({overlapping?.StartDate} to {overlapping?.EndDate})");
            }

            try
            {
                context.ChampionshipConfigurations.Add(config);
                await context.SaveChangesAsync();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration added: {Name} ({StartDate} to {EndDate})",
                        config.Name, config.StartDate, config.EndDate);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error adding championship configuration");
                }
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<(bool success, string? errorMessage)> UpdateAsync(ChampionshipConfiguration config)
        {
            var (isValid, validationError) = config.Validate();
            if (!isValid)
            {
                return (false, validationError);
            }

            var existing = await GetByIdAsync(config.Id);
            if (existing == null)
            {
                return (false, "Championship configuration not found");
            }

            if (await HasOverlappingConfigurationsAsync(config))
            {
                var overlapping = await GetOverlappingConfigurationAsync(config);
                return (false, $"Championship period overlaps with existing championship '{overlapping?.Name}' ({overlapping?.StartDate} to {overlapping?.EndDate})");
            }

            try
            {
                existing.Name = config.Name;
                existing.StartDate = config.StartDate;
                existing.EndDate = config.EndDate;

                await context.SaveChangesAsync();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration updated: {Id} - {Name}", config.Id, config.Name);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error updating championship configuration");
                }
                return (false, $"Database error: {ex.Message}");
            }
        }

        public async Task<bool> RemoveAsync(string id)
        {
            var config = await GetByIdAsync(id);
            if (config == null)
            {
                return false;
            }

            try
            {
                context.ChampionshipConfigurations.Remove(config);
                await context.SaveChangesAsync();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Championship configuration removed: {Id} - {Name}", id, config.Name);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error removing championship configuration");
                }
                return false;
            }
        }

        public async Task<bool> HasOverlappingConfigurationsAsync(ChampionshipConfiguration config)
        {
            return await context.ChampionshipConfigurations
                .AnyAsync(c => c.Id != config.Id &&
                          c.StartDate <= config.EndDate &&
                          c.EndDate >= config.StartDate);
        }

        private async Task<ChampionshipConfiguration?> GetOverlappingConfigurationAsync(ChampionshipConfiguration config)
        {
            return await context.ChampionshipConfigurations
                .FirstOrDefaultAsync(c => c.Id != config.Id &&
                                     c.StartDate <= config.EndDate &&
                                     c.EndDate >= config.StartDate);
        }
    }
}
