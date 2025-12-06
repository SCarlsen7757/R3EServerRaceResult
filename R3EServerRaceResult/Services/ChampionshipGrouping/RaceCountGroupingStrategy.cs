using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class RaceCountGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly int _racesPerChampionship;
        private readonly DateTime? _championshipStartDate;
        private readonly Dictionary<string, int> _raceCountTracker = new();
        private readonly object _lock = new();

        public RaceCountGroupingStrategy(int racesPerChampionship, DateTime? championshipStartDate = null)
        {
            if (racesPerChampionship <= 0)
            {
                throw new ArgumentException("Races per championship must be greater than 0", nameof(racesPerChampionship));
            }
            _racesPerChampionship = racesPerChampionship;
            _championshipStartDate = championshipStartDate;
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
            
            lock (_lock)
            {
                if (!_raceCountTracker.ContainsKey(yearKey))
                {
                    _raceCountTracker[yearKey] = 0;
                }
                
                var championshipNumber = CalculateChampionshipNumber(raceResult.StartTime);
                var raceNumber = (_raceCountTracker[yearKey] % _racesPerChampionship) + 1;
                
                _raceCountTracker[yearKey]++;
                
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
            
            if (_championshipStartDate == null)
            {
                lock (_lock)
                {
                    if (!_raceCountTracker.ContainsKey(raceYearKey))
                    {
                        _raceCountTracker[raceYearKey] = 0;
                    }
                    
                    return (_raceCountTracker[raceYearKey] / _racesPerChampionship) + 1;
                }
            }

            var startDate = _championshipStartDate.Value;
            var daysSinceStart = (raceDate.Date - startDate.Date).Days;
            
            if (daysSinceStart < 0)
            {
                return 1;
            }
            
            lock (_lock)
            {
                if (!_raceCountTracker.ContainsKey(raceYearKey))
                {
                    _raceCountTracker[raceYearKey] = 0;
                }
                
                var totalRacesSinceStart = _raceCountTracker[raceYearKey];
                return (totalRacesSinceStart / _racesPerChampionship) + 1;
            }
        }
    }
}
