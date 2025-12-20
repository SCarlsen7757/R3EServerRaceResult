namespace R3EServerRaceResult.Models.R3EContent;

public class Track
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public ICollection<Layout> Layouts { get; set; } = [];
}
