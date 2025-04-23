using System.Text.Json;
using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    public class ListTimeSpanConverter : JsonConverter<List<TimeSpan>>
    {
        public override List<TimeSpan> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Deserialize the array of TimeSpan (in milliseconds) into a List<TimeSpan>
            var list = JsonSerializer.Deserialize<List<long>>(ref reader, options);
            var timeSpans = new List<TimeSpan>();

            // Convert each millisecond value into a TimeSpan
            foreach (var milliseconds in list)
            {
                timeSpans.Add(TimeSpan.FromMilliseconds(milliseconds));
            }

            return timeSpans;
        }

        public override void Write(Utf8JsonWriter writer, List<TimeSpan> value, JsonSerializerOptions options)
        {
            // Convert List<TimeSpan> back to list of milliseconds
            var list = new List<long>();
            foreach (var timeSpan in value)
            {
                list.Add((long)timeSpan.TotalMilliseconds);
            }

            JsonSerializer.Serialize(writer, list, options);
        }
    }
}
