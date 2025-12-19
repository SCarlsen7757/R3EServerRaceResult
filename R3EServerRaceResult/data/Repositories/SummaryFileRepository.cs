using Microsoft.EntityFrameworkCore;
using R3EServerRaceResult.Models;
using R3EServerRaceResult.Settings;

namespace R3EServerRaceResult.Data.Repositories
{
    public class SummaryFileRepository : ISummaryFileRepository
    {
        private readonly ChampionshipDbContext context;
        private readonly ILogger<SummaryFileRepository> logger;

        public SummaryFileRepository(
            ChampionshipDbContext context,
            ILogger<SummaryFileRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<SummaryFile?> GetByFilePathAsync(string filePath)
        {
            return await context.SummaryFiles
                .FirstOrDefaultAsync(s => s.FilePath == filePath);
        }

        public async Task<SummaryFile?> GetByChampionshipKeyAsync(string championshipKey)
        {
            return await context.SummaryFiles
                .FirstOrDefaultAsync(s => s.ChampionshipKey == championshipKey);
        }

        public async Task<List<SummaryFile>> GetAllAsync(int? year = null, GroupingStrategyType? strategy = null)
        {
            var query = context.SummaryFiles.AsQueryable();

            if (year.HasValue)
            {
                query = query.Where(s => s.Year == year.Value);
            }

            if (strategy.HasValue)
            {
                query = query.Where(s => s.Strategy == strategy.Value);
            }

            return await query
                .OrderByDescending(s => s.Year)
                .ThenBy(s => s.ChampionshipKey)
                .ToListAsync();
        }

        public async Task<bool> AddOrUpdateAsync(SummaryFile summaryFile)
        {
            try
            {
                var existing = await GetByFilePathAsync(summaryFile.FilePath);

                if (existing == null)
                {
                    // Add new
                    context.SummaryFiles.Add(summaryFile);

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Adding new summary file index: {FilePath}", summaryFile.FilePath);
                    }
                }
                else
                {
                    // Update existing
                    existing.ChampionshipKey = summaryFile.ChampionshipKey;
                    existing.ChampionshipName = summaryFile.ChampionshipName;
                    existing.Strategy = summaryFile.Strategy;
                    existing.Year = summaryFile.Year;
                    existing.RaceCount = summaryFile.RaceCount;
                    existing.LastUpdated = DateTime.UtcNow;

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Updating summary file index: {FilePath} (RaceCount: {RaceCount})", 
                            summaryFile.FilePath, summaryFile.RaceCount);
                    }
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error adding/updating summary file index: {FilePath}", summaryFile.FilePath);
                }
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string filePath)
        {
            try
            {
                var existing = await GetByFilePathAsync(filePath);
                if (existing == null)
                {
                    return false;
                }

                context.SummaryFiles.Remove(existing);
                await context.SaveChangesAsync();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Deleted summary file index: {FilePath}", filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Error deleting summary file index: {FilePath}", filePath);
                }
                return false;
            }
        }

        public async Task<int> GetCountAsync()
        {
            return await context.SummaryFiles.CountAsync();
        }
    }
}
