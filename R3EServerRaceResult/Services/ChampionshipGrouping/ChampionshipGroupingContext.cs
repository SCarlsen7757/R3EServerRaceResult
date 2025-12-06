using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class ChampionshipGroupingContext
    {
        public Result RaceResult { get; set; } = null!;
        public int RaceCount { get; set; }
        public string? CarClass { get; set; }
    }
}
