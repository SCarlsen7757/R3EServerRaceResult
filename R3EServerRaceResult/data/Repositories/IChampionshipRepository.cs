using R3EServerRaceResult.Models;

namespace R3EServerRaceResult.Data.Repositories
{
    public interface IChampionshipRepository
    {
        Task<ChampionshipConfiguration?> GetByIdAsync(string id);
        Task<ChampionshipConfiguration?> GetConfigurationForDateAsync(DateTime date);
        Task<List<ChampionshipConfiguration>> GetAllAsync(bool includeExpired = true);
        Task<(bool success, string? errorMessage)> AddAsync(ChampionshipConfiguration config);
        Task<(bool success, string? errorMessage)> UpdateAsync(ChampionshipConfiguration config);
        Task<bool> RemoveAsync(string id);
        Task<bool> HasOverlappingConfigurationsAsync(ChampionshipConfiguration config);
    }
}
