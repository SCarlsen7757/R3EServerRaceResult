using R3EServerRaceResult.Models;

namespace R3EServerRaceResult.Data.Repositories
{
    public interface IRaceCountRepository
    {
        Task<RaceCountState?> GetByYearAsync(int year);
        Task<int> IncrementRaceCountAsync(int year, int racesPerChampionship);
        Task<Dictionary<int, int>> GetAllRaceCountsAsync();
        Task<bool> ValidateConfigurationAsync(int year, int racesPerChampionship);
        Task ResetCountForYearAsync(int year, int racesPerChampionship, string? reason = null);
    }
}
