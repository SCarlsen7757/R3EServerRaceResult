using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EContent.Json;

public class LiveryJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }
    
    [JsonPropertyName("Experiences")]
    public List<int> Experiences { get; set; } = [];
}

public class CarJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Class")]
    public int Class { get; set; }
    
    [JsonPropertyName("Experiences")]
    public List<int> Experiences { get; set; } = [];
}

public class CarWithLiveriesJson
{
    [JsonPropertyName("Car")]
    public CarJson Car { get; set; } = new();
    
    [JsonPropertyName("Liveries")]
    public List<LiveryJson> Liveries { get; set; } = [];
}

public class LayoutJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Experiences")]
    public List<int> Experiences { get; set; } = [];
    
    [JsonPropertyName("MaxVehicles")]
    public int MaxVehicles { get; set; }
}

public class TrackJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Layouts")]
    public List<LayoutJson> Layouts { get; set; } = [];
    
    [JsonPropertyName("Experiences")]
    public List<int> Experiences { get; set; } = [];
}

public class ContentRootJson
{
    [JsonPropertyName("Tracks")]
    public List<TrackJson> Tracks { get; set; } = [];
    
    [JsonPropertyName("Liveries")]
    public List<CarWithLiveriesJson> Liveries { get; set; } = [];
}
