using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public record Result(
        [property: JsonPropertyName("Server")] string Server,
        [property: JsonPropertyName("StartTime"), JsonConverter(typeof(UnixTimestampConverter))] DateTime StartTime,
        [property: JsonPropertyName("Time"), JsonConverter(typeof(UnixTimestampConverter))] DateTime Time,
        [property: JsonPropertyName("Experience")] string Experience,
        [property: JsonPropertyName("Difficulty")] string Difficulty,
        [property: JsonPropertyName("FuelUsage")] string FuelUsage,
        [property: JsonPropertyName("TireWear")] string TireWear,
        [property: JsonPropertyName("MechanicalDamage")] object? MechanicalDamage,
        [property: JsonPropertyName("FlagRules")] string FlagRules,
        [property: JsonPropertyName("CutRules")] string CutRules,
        [property: JsonPropertyName("RaceSeriesFormat")] object? RaceSeriesFormat,
        [property: JsonPropertyName("WreckerPrevention")] string WreckerPrevention,
        [property: JsonPropertyName("MandatoryPitstop")] string MandatoryPitstop,
        [property: JsonPropertyName("Track")] string Track,
        [property: JsonPropertyName("TrackLayout")] string TrackLayout,
        [property: JsonPropertyName("Sessions")] List<Session> Sessions
    );
}
