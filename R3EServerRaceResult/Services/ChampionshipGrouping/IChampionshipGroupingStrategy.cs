using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public interface IChampionshipGroupingStrategy
    {
        string GetChampionshipKey(Result raceResult);
        string GetEventName(Result raceResult);
        string GetSummaryFolder(Result raceResult);
    }
}
