using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class MonthlyGroupingStrategy : IChampionshipGroupingStrategy
    {
        public Task<string> GetChampionshipKeyAsync(Result raceResult)
        {
            return Task.FromResult($"{raceResult.StartTime:yyyy-MM}");
        }

        public Task<string> GetEventNameAsync(Result raceResult)
        {
            return Task.FromResult($"{raceResult.StartTime:MMMM} Race {raceResult.StartTime:yyyy}");
        }

        public Task<string> GetSummaryFolderAsync(Result raceResult)
        {
            return Task.FromResult(raceResult.StartTime.Year.ToString());
        }
    }
}
