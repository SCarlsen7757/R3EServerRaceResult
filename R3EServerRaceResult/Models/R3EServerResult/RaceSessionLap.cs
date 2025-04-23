using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public class RaceSessionLap
    {
        [JsonPropertyName("Time")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Time { get; set; }

        [JsonPropertyName("SectorTimes")]
        [JsonConverter(typeof(ListTimeSpanConverter))]
        public List<TimeSpan> SectorTimes { get; set; } = [];

        [JsonPropertyName("PositionInClass")]
        public int PositionInClass { get; set; }

        [JsonPropertyName("Valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("Position")]
        public int Position { get; set; }

        [JsonPropertyName("PitStopOccured")]
        public bool PitStopOccured { get; set; }

        [JsonPropertyName("Incidents")]
        public List<Incident> Incidents { get; set; } = [];
    }


}
