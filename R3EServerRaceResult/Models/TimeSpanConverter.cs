using System.Text.Json;
using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the value (milliseconds) from the JSON
            long milliseconds = reader.GetInt64();

            // Convert milliseconds to TimeSpan
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            // Convert TimeSpan to milliseconds before writing to JSON (optional)
            writer.WriteNumberValue((long)value.TotalMilliseconds);
        }
    }
}
