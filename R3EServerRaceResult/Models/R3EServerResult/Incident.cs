using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public class Incident
    {
        [JsonPropertyName("Type")]
        public int Type { get; set; }

        [JsonPropertyName("Points")]
        public int Points { get; set; }

        [JsonPropertyName("OtherUserId")]
        public int OtherUserId { get; set; }
    }
}
