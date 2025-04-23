using System.Text.Json;
using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    public class UnixTimestampConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the UNIX timestamp (in seconds)
            long unixTimestamp = reader.GetInt64();

            // Convert UNIX timestamp to DateTime (UTC)
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Convert DateTime to UNIX timestamp (seconds) before serializing (optional)
            long unixTimestamp = ((DateTimeOffset)value).ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTimestamp);
        }
    }
}
