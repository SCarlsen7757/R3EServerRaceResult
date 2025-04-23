using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public class Player
    {
        [JsonPropertyName("UserId")]
        public int UserId { get; set; }

        [JsonPropertyName("FullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("UserWeightPenalty")]
        public int UserWeightPenalty { get; set; }

        [JsonPropertyName("CarId")]
        public int CarId { get; set; }

        [JsonPropertyName("Car")]
        public string Car { get; set; } = string.Empty;

        [JsonPropertyName("CarWeightPenalty")]
        public int CarWeightPenalty { get; set; }

        [JsonPropertyName("LiveryId")]
        public int LiveryId { get; set; }

        [JsonPropertyName("CarPerformanceIndex")]
        public int CarPerformanceIndex { get; set; }

        [JsonPropertyName("Position")]
        public int Position { get; set; }

        [JsonPropertyName("PositionInClass")]
        public int PositionInClass { get; set; }

        [JsonPropertyName("StartPosition")]
        public int StartPosition { get; set; }

        [JsonPropertyName("StartPositionInClass")]
        public int StartPositionInClass { get; set; }

        [JsonPropertyName("BestLapTime")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan BestLapTime { get; set; }

        [JsonPropertyName("TotalTime")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan TotalTime { get; set; }

        [JsonPropertyName("FinishStatus")]
        public string FinishStatus { get; set; } = string.Empty;

        [JsonPropertyName("RaceSessionLaps")]
        public List<RaceSessionLap> RaceSessionLaps { get; set; } = [];
    }
}
