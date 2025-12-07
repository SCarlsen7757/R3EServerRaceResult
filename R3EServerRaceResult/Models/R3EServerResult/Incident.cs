using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public record Incident(
        [property: JsonPropertyName("Type")] int Type,
        [property: JsonPropertyName("Points")] int Points,
        [property: JsonPropertyName("OtherUserId")] int OtherUserId
    );
}
