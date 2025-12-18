using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public record RaceSessionLap(
        [property: JsonPropertyName("Time"), JsonConverter(typeof(TimeSpanConverter))] TimeSpan Time,
        [property: JsonPropertyName("SectorTimes"), JsonConverter(typeof(ListTimeSpanConverter))] List<TimeSpan> SectorTimes,
        [property: JsonPropertyName("PositionInClass")] int PositionInClass,
        [property: JsonPropertyName("Valid")] bool Valid,
        [property: JsonPropertyName("Position")] int Position,
        [property: JsonPropertyName("PitStopOccured")] bool PitStopOccured,
        [property: JsonPropertyName("Incidents")] List<Incident> Incidents
    );
}
