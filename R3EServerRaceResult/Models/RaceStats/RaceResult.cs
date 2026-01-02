namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Race result entity for a driver in a session
/// </summary>
public class RaceResult
{
    /// <summary>
    /// Auto-generated ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Driver
    /// </summary>
    public int DriverId { get; set; }

    /// <summary>
    /// Foreign key to Session
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Foreign key to Car (from R3E Content)
    /// </summary>
    public int CarId { get; set; }

    /// <summary>
    /// Starting position (overall)
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Final position (overall)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Starting position in class
    /// </summary>
    public int ClassStartPosition { get; set; }

    /// <summary>
    /// Final position in class
    /// </summary>
    public int ClassPosition { get; set; }

    /// <summary>
    /// Total race time in milliseconds
    /// </summary>
    public long TotalRaceTime { get; set; }

    /// <summary>
    /// Best lap time in milliseconds (null if no valid lap was set)
    /// </summary>
    public long? BestLapTime { get; set; }

    /// <summary>
    /// Finish status (Finished, DNF, DSQ, etc.)
    /// </summary>
    public string FinishStatus { get; set; } = string.Empty;

    /// <summary>
    /// Total number of laps completed
    /// </summary>
    public int TotalLaps { get; set; }

    // Navigation properties
    public Driver Driver { get; set; } = null!;
    public RaceSession Session { get; set; } = null!;
}
