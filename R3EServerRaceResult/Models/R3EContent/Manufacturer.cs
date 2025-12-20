namespace R3EServerRaceResult.Models.R3EContent;

public class Manufacturer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public ICollection<Car> Cars { get; set; } = [];
}
