using R3EServerRaceResult.Models;
using R3EServerRaceResult.Settings;

namespace R3EServerRaceResult.Data.Repositories
{
    public interface ISummaryFileRepository
    {
        Task<Summary?> GetByFilePathAsync(string filePath);
        Task<Summary?> GetByChampionshipKeyAsync(string championshipKey);
        Task<List<Summary>> GetAllAsync(int? year = null, GroupingStrategyType? strategy = null);
        Task<bool> AddOrUpdateAsync(Summary summaryFile);
        Task<bool> DeleteAsync(string filePath);
        Task<int> GetCountAsync();
    }
}
