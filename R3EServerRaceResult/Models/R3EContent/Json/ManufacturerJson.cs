using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EContent.Json;

public class ManufacturerJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("CarIds")]
    public List<int> CarIds { get; set; } = [];
}

public class ManufacturersRootJson
{
    [JsonPropertyName("Manufacturers")]
    public List<ManufacturerJson> Manufacturers { get; set; } = [];
}
