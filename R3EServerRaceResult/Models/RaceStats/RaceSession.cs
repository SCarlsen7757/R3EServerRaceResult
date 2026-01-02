namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Race session entity (Practice, Qualify, or Race)
/// </summary>
public class RaceSession
{
    /// <summary>
    /// Auto-generated ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Event
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Session type (Practice, Qualify, or Race)
    /// </summary>
    public SessionType SessionType { get; set; }

    /// <summary>
    /// Session number - For Practice and Qualify it's 0. For Race it's 1 to 3
    /// </summary>
    public int SessionNumber { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<RaceResult> Results { get; set; } = [];
    public ICollection<Lap> Laps { get; set; } = [];
    public ICollection<RaceIncident> Incidents { get; set; } = [];
}
