using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public record Session(
        [property: JsonPropertyName("Type")] string Type,
        [property: JsonPropertyName("Players")] List<Player> Players
    );
}
