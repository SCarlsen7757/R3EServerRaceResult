using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    /// <summary>
    /// Request to reset race counter for a specific year
    /// </summary>
    public record ResetRaceCountRequest(
        [property: JsonPropertyName("year")] int? Year,
        [property: JsonPropertyName("reason")] string? Reason
    );

    /// <summary>
    /// Response after resetting race counter
    /// </summary>
    public record ResetRaceCountResponse(
        [property: JsonPropertyName("year")] int Year,
        [property: JsonPropertyName("previousCount")] int PreviousCount,
        [property: JsonPropertyName("newCount")] int NewCount,
        [property: JsonPropertyName("previousChampionship")] string? PreviousChampionship,
        [property: JsonPropertyName("nextChampionship")] string NextChampionship,
        [property: JsonPropertyName("message")] string Message
    );

    /// <summary>
    /// Current race count state for a year
    /// </summary>
    public record RaceCountStateResponse(
        [property: JsonPropertyName("year")] int Year,
        [property: JsonPropertyName("raceCount")] int RaceCount,
        [property: JsonPropertyName("racesPerChampionship")] int RacesPerChampionship,
        [property: JsonPropertyName("currentChampionship")] string CurrentChampionship,
        [property: JsonPropertyName("nextRaceNumber")] int NextRaceNumber,
        [property: JsonPropertyName("lastUpdated")] DateTime LastUpdated
    );
}
