namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Driver entity for race statistics
/// Stores driver information with shortened name for privacy
/// </summary>
public class Driver
{
    /// <summary>
    /// User ID from R3E
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Shortened name for privacy (e.g., "J. Smith")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<RaceResult> Results { get; set; } = [];
    public ICollection<Lap> Laps { get; set; } = [];
    public ICollection<RaceIncident> Incidents { get; set; } = [];
    public ICollection<RaceIncident> InvolvedIncidents { get; set; } = [];
}
