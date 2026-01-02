namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Event entity representing a race event at a specific track
/// </summary>
public class Event
{
    /// <summary>
    /// Auto-generated ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Track (from R3E Content)
    /// </summary>
    public int TrackId { get; set; }

    /// <summary>
    /// Foreign key to Layout (from R3E Content)
    /// </summary>
    public int LayoutId { get; set; }

    /// <summary>
    /// Date of the event
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Server name where the event was hosted
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<RaceSession> Sessions { get; set; } = [];
}
