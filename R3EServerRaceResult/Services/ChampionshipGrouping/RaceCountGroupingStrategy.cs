using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class RaceCountGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly int racesPerChampionship;
        private readonly DateTime? championshipStartDate;
        private readonly Dictionary<string, int> raceCountTracker = [];
        private readonly Lock @lock = new();

        public RaceCountGroupingStrategy(int racesPerChampionship, DateTime? championshipStartDate = null)
        {
            if (racesPerChampionship <= 0)
            {
                throw new ArgumentException("Races per championship must be greater than 0", nameof(racesPerChampionship));
            }
            this.racesPerChampionship = racesPerChampionship;
            this.championshipStartDate = championshipStartDate;
        }

        public string GetChampionshipKey(Result raceResult)
        {
            var championshipNumber = CalculateChampionshipNumber(raceResult.StartTime);
            var year = raceResult.StartTime.Year;
            return $"{year}-C{championshipNumber:D2}";
        }

        public string GetEventName(Result raceResult)
        {
            var year = raceResult.StartTime.Year;
            var yearKey = year.ToString();

            lock (@lock)
            {
                if (!raceCountTracker.ContainsKey(yearKey))
                {
                    raceCountTracker.TryAdd(yearKey, 0);
                }

                var championshipNumber = CalculateChampionshipNumber(raceResult.StartTime);
                var raceNumber = (raceCountTracker[yearKey] % racesPerChampionship) + 1;

                raceCountTracker[yearKey]++;

                return $"Championship {championshipNumber} - Race {raceNumber} ({year})";
            }
        }

        public string GetStoragePath(Result raceResult)
        {
            var year = raceResult.StartTime.Year;
            var championshipNumber = CalculateChampionshipNumber(raceResult.StartTime);
            return Path.Combine(year.ToString(), $"Championship-{championshipNumber:D2}");
        }

        private int CalculateChampionshipNumber(DateTime raceDate)
        {
            var raceYear = raceDate.Year;
            var raceYearKey = raceYear.ToString();

            if (championshipStartDate == null)
            {
                lock (@lock)
                {
                    if (!raceCountTracker.ContainsKey(raceYearKey))
                    {
                        raceCountTracker.TryAdd(raceYearKey, 0);
                    }

                    return (raceCountTracker[raceYearKey] / racesPerChampionship) + 1;
                }
            }

            var startDate = championshipStartDate.Value;
            var daysSinceStart = (raceDate.Date - startDate.Date).Days;

            if (daysSinceStart < 0)
            {
                return 1;
            }

            lock (@lock)
            {
                if (!raceCountTracker.ContainsKey(raceYearKey))
                {
                    raceCountTracker.TryAdd(raceYearKey, 0);
                }

                var totalRacesSinceStart = raceCountTracker[raceYearKey];
                return (totalRacesSinceStart / racesPerChampionship) + 1;
            }
        }
    }
}
