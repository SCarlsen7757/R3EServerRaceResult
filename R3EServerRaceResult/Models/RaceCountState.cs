namespace R3EServerRaceResult.Models
{
    /// <summary>
    /// Entity for tracking race counts per year for RaceCount grouping strategy
    /// </summary>
    public class RaceCountState
    {
        /// <summary>
        /// Year (e.g., 2025)
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Number of races processed for this year
        /// </summary>
        public int RaceCount { get; set; }

        /// <summary>
        /// Configuration: Number of races per championship
        /// Used to detect configuration changes
        /// </summary>
        public int RacesPerChampionship { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
