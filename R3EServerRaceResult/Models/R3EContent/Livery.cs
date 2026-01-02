namespace R3EServerRaceResult.Models.R3EContent;

public class Livery
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}
