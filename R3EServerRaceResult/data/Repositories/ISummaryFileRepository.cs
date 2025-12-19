using R3EServerRaceResult.Models;
using R3EServerRaceResult.Settings;

namespace R3EServerRaceResult.Data.Repositories
{
    public interface ISummaryFileRepository
    {
        Task<SummaryFile?> GetByFilePathAsync(string filePath);
        Task<SummaryFile?> GetByChampionshipKeyAsync(string championshipKey);
        Task<List<SummaryFile>> GetAllAsync(int? year = null, GroupingStrategyType? strategy = null);
        Task<bool> AddOrUpdateAsync(SummaryFile summaryFile);
        Task<bool> DeleteAsync(string filePath);
        Task<int> GetCountAsync();
    }
}
