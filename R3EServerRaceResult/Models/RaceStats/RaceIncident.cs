using R3EServerRaceResult.Models.R3EServerResult;

namespace R3EServerRaceResult.Models.RaceStats;

/// <summary>
/// Incident entity for tracking race incidents
/// </summary>
public class RaceIncident
{
    /// <summary>
    /// Auto-generated ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Driver who caused/received the incident
    /// </summary>
    public int DriverId { get; set; }

    /// <summary>
    /// Foreign key to Session
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Type of incident
    /// </summary>
    public IncidentType IncidentType { get; set; }

    /// <summary>
    /// Incident points
    /// </summary>
    public int IncidentPoints { get; set; }

    /// <summary>
    /// Lap number when the incident occurred
    /// </summary>
    public int LapNumber { get; set; }

    /// <summary>
    /// Foreign key to involved driver (null if solo incident)
    /// </summary>
    public int? InvolvedDriverId { get; set; }

    // Navigation properties
    public Driver Driver { get; set; } = null!;
    public RaceSession Session { get; set; } = null!;
    public Driver? InvolvedDriver { get; set; }
}
