using R3EServerRaceResult.Models.R3EServerResult;
using System.Threading.Tasks;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public interface IChampionshipGroupingStrategy
    {
        Task<string> GetChampionshipKeyAsync(Result raceResult);
        Task<string> GetEventNameAsync(Result raceResult);
        Task<string> GetSummaryFolderAsync(Result raceResult);
    }
}
