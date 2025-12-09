using R3EServerRaceResult.Models.R3EServerResult;
using System.Text.Json;

namespace R3EServerRaceResult.Services.ChampionshipGrouping
{
    public class RaceCountGroupingStrategy : IChampionshipGroupingStrategy
    {
        private readonly int racesPerChampionship;
        private readonly DateTime? championshipStartDate;
        private readonly string stateFilePath;
        private readonly Dictionary<string, int> raceCountTracker = [];
        private readonly Lock @lock = new();

        public RaceCountGroupingStrategy(int racesPerChampionship, DateTime? championshipStartDate, string mountedVolumePath)
        {
            if (racesPerChampionship <= 0)
            {
                throw new ArgumentException("Races per championship must be greater than 0", nameof(racesPerChampionship));
            }

            this.racesPerChampionship = racesPerChampionship;
            this.championshipStartDate = championshipStartDate;
            stateFilePath = Path.Combine(mountedVolumePath, ".racecount_state.json");

            LoadState();
        }

        public string GetChampionshipKey(Result raceResult)
        {
            var championshipNumber = GetChampionshipNumber(raceResult.StartTime);
            var year = raceResult.StartTime.Year;
            return $"{year}-C{championshipNumber:D2}";
        }

        public string GetEventName(Result raceResult)
        {
            var year = raceResult.StartTime.Year;
            var championshipNumber = GetChampionshipNumber(raceResult.StartTime);
            var raceNumber = GetRaceNumber(year);

            IncrementRaceCount(year);

            return $"Championship {championshipNumber} - Race {raceNumber} ({year})";
        }

        public string GetChampionshipFolder(Result raceResult)
        {
            var year = raceResult.StartTime.Year;
            var championshipNumber = GetChampionshipNumber(raceResult.StartTime);
            return Path.Combine(year.ToString(), $"champ{championshipNumber}");
        }

        private int GetChampionshipNumber(DateTime raceDate)
        {
            var raceYear = raceDate.Year;
            var raceYearKey = raceYear.ToString();

            if (championshipStartDate == null)
            {
                lock (@lock)
                {
                    if (!raceCountTracker.ContainsKey(raceYearKey))
                    {
                        raceCountTracker[raceYearKey] = 0;
                    }

                    return (raceCountTracker[raceYearKey] / racesPerChampionship) + 1;
                }
            }

            var startDate = championshipStartDate.Value;
            if (raceDate.Date < startDate.Date)
            {
                return 1;
            }

            lock (@lock)
            {
                if (!raceCountTracker.ContainsKey(raceYearKey))
                {
                    raceCountTracker[raceYearKey] = 0;
                }

                return (raceCountTracker[raceYearKey] / racesPerChampionship) + 1;
            }
        }

        private int GetRaceNumber(int year)
        {
            var yearKey = year.ToString();

            lock (@lock)
            {
                if (!raceCountTracker.ContainsKey(yearKey))
                {
                    raceCountTracker[yearKey] = 0;
                }

                return (raceCountTracker[yearKey] % racesPerChampionship) + 1;
            }
        }

        private void IncrementRaceCount(int year)
        {
            var yearKey = year.ToString();

            lock (@lock)
            {
                if (!raceCountTracker.ContainsKey(yearKey))
                {
                    raceCountTracker[yearKey] = 0;
                }

                raceCountTracker[yearKey]++;
                SaveState();
            }
        }

        private void LoadState()
        {
            lock (@lock)
            {
                try
                {
                    if (File.Exists(stateFilePath))
                    {
                        var json = File.ReadAllText(stateFilePath);
                        var state = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                        if (state != null)
                        {
                            foreach (var kvp in state)
                            {
                                raceCountTracker[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // If state file is corrupted, start fresh
                    raceCountTracker.Clear();
                }
            }
        }

        private void SaveState()
        {
            try
            {
                var tempPath = stateFilePath + ".tmp";
                var json = JsonSerializer.Serialize(raceCountTracker, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);

                if (File.Exists(stateFilePath))
                {
                    File.Delete(stateFilePath);
                }

                File.Move(tempPath, stateFilePath);
            }
            catch (Exception)
            {
                // Log error if needed, but don't throw to avoid breaking race result upload
            }
        }
    }
}
