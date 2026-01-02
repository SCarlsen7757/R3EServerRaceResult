namespace R3EServerRaceResult.Models.R3EContent;

public class CarClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Car> Cars { get; set; } = [];
}
