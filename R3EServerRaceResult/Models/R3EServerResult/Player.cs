using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public record Player(
        [property: JsonPropertyName("UserId")] int UserId,
        [property: JsonPropertyName("FullName")] string FullName,
        [property: JsonPropertyName("Username")] string Username,
        [property: JsonPropertyName("UserWeightPenalty")] int UserWeightPenalty,
        [property: JsonPropertyName("CarId")] int CarId,
        [property: JsonPropertyName("Car")] string Car,
        [property: JsonPropertyName("CarWeightPenalty")] int CarWeightPenalty,
        [property: JsonPropertyName("LiveryId")] int LiveryId,
        [property: JsonPropertyName("CarPerformanceIndex")] int CarPerformanceIndex,
        [property: JsonPropertyName("Position")] int Position,
        [property: JsonPropertyName("PositionInClass")] int PositionInClass,
        [property: JsonPropertyName("StartPosition")] int StartPosition,
        [property: JsonPropertyName("StartPositionInClass")] int StartPositionInClass,
        [property: JsonPropertyName("BestLapTime"), JsonConverter(typeof(TimeSpanConverter))] TimeSpan BestLapTime,
        [property: JsonPropertyName("TotalTime"), JsonConverter(typeof(TimeSpanConverter))] TimeSpan TotalTime,
        [property: JsonPropertyName("FinishStatus")] string FinishStatus,
        [property: JsonPropertyName("RaceSessionLaps")] List<RaceSessionLap> RaceSessionLaps
    );
}
