namespace R3EServerRaceResult.Models.R3EContent;

public class Car
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public int ClassId { get; set; }
    public CarClass Class { get; set; } = null!;
    
    public int ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; } = null!;

    public ICollection<Livery> Liveries { get; set; } = [];
}
