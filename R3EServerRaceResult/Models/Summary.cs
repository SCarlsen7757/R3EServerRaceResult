using R3EServerRaceResult.Settings;

namespace R3EServerRaceResult.Models
{
    /// <summary>
    /// Entity for indexing summary files in the database
    /// </summary>
    public class Summary
    {
        /// <summary>
        /// Unique identifier (GUID)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Relative path to summary file (e.g., "2025/champ1/summary.json")
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Championship key (e.g., "2025-C01")
        /// </summary>
        public string ChampionshipKey { get; set; } = string.Empty;

        /// <summary>
        /// Championship name (e.g., "Championship 1 - 2025")
        /// </summary>
        public string? ChampionshipName { get; set; }

        /// <summary>
        /// Grouping strategy used (Monthly, RaceCount, Custom)
        /// </summary>
        public GroupingStrategyType Strategy { get; set; }

        /// <summary>
        /// Year of the championship
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Number of race events in this championship
        /// </summary>
        public int RaceCount { get; set; }

        /// <summary>
        /// When the summary was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the summary was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
