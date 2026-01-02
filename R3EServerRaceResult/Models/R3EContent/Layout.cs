namespace R3EServerRaceResult.Models.R3EContent;

public class Layout
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxVehicles { get; set; }
    
    public int TrackId { get; set; }
    public Track Track { get; set; } = null!;
}
