using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public class Result
    {
        [JsonPropertyName("Server")]
        public string Server { get; set; } = string.Empty;

        [JsonPropertyName("StartTime")]
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("Time")]
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime Time { get; set; }

        [JsonPropertyName("Experience")]
        public string Experience { get; set; } = string.Empty;

        [JsonPropertyName("Difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("FuelUsage")]
        public string FuelUsage { get; set; } = string.Empty;

        [JsonPropertyName("TireWear")]
        public string TireWear { get; set; } = string.Empty;

        [JsonPropertyName("MechanicalDamage")]
        public object? MechanicalDamage { get; set; }

        [JsonPropertyName("FlagRules")]
        public string FlagRules { get; set; } = string.Empty;

        [JsonPropertyName("CutRules")]
        public string CutRules { get; set; } = string.Empty;

        [JsonPropertyName("RaceSeriesFormat")]
        public object? RaceSeriesFormat { get; set; }

        [JsonPropertyName("WreckerPrevention")]
        public string WreckerPrevention { get; set; } = string.Empty;

        [JsonPropertyName("MandatoryPitstop")]
        public string MandatoryPitstop { get; set; } = string.Empty;

        [JsonPropertyName("Track")]
        public string Track { get; set; } = string.Empty;

        [JsonPropertyName("TrackLayout")]
        public string TrackLayout { get; set; } = string.Empty;

        [JsonPropertyName("Sessions")]
        public List<Session> Sessions { get; set; } = [];
    }
}
