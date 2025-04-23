using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.SimResult
{
    public class Result
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("log")]
        public List<string> Log { get; set; } = [];
    }
}
