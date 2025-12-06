using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class MonthlyGroupingStrategy : IChampionshipGroupingStrategy
    {
        public string GetChampionshipKey(Result raceResult)
        {
            return $"{raceResult.StartTime:yyyy-MM}";
        }

        public string GetEventName(Result raceResult)
        {
            return $"{raceResult.StartTime:MMMM} Race {raceResult.StartTime:yyyy}";
        }

        public string GetStoragePath(Result raceResult)
        {
            return Path.Combine(raceResult.StartTime.Year.ToString(), raceResult.StartTime.Month.ToString());
        }
    }
}
