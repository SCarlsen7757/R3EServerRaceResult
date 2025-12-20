using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EContent.Json;

public class TrackLocationJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Location")]
    public string Location { get; set; } = string.Empty;
}

public class TrackLocationsRootJson
{
    [JsonPropertyName("Tracks")]
    public List<TrackLocationJson> Tracks { get; set; } = [];
}
