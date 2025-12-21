namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Lap entity for tracking individual lap times
/// </summary>
public class Lap
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
    /// Lap number (1-based)
    /// </summary>
    public int LapNumber { get; set; }

    /// <summary>
    /// Lap time in milliseconds (null if lap was not completed)
    /// </summary>
    public long? LapTime { get; set; }

    /// <summary>
    /// Sector 1 time in milliseconds (null if sector not completed)
    /// </summary>
    public long? Sector1Time { get; set; }

    /// <summary>
    /// Sector 2 time in milliseconds (null if sector not completed)
    /// </summary>
    public long? Sector2Time { get; set; }

    /// <summary>
    /// Sector 3 time in milliseconds (null if sector not completed)
    /// </summary>
    public long? Sector3Time { get; set; }

    /// <summary>
    /// Whether the lap was valid (no cuts, etc.)
    /// </summary>
    public bool IsValid { get; set; }

    // Navigation properties
    public Driver Driver { get; set; } = null!;
    public RaceSession Session { get; set; } = null!;
}
