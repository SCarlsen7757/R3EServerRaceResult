using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EContent.Json;

public class TrackCountryJson
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("CountryCode")]
    public string CountryCode { get; set; } = string.Empty;
}

public class TrackLocationsRootJson
{
    [JsonPropertyName("Tracks")]
    public List<TrackCountryJson> Tracks { get; set; } = [];
}
