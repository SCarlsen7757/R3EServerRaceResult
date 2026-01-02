using System.Text.Json;
using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.R3EServerResult
{
    public sealed class IncidentTypeJsonConverter : JsonConverter<IncidentType>
    {
        public override IncidentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetInt32();
            return (IncidentType)value;
        }

        public override void Write(Utf8JsonWriter writer, IncidentType value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
