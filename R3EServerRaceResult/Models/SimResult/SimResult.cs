using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.SimResult
{
    public class SimResult
    {
        public SimResult() { }

        public SimResult(Settings.ChampionshipAppSettings settings)
        {
            Config = new(settings);
        }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("results")]
        public List<Result> Results { get; set; } = [];

        [JsonPropertyName("config")]
        public Config? Config { get; set; }
    }
}
