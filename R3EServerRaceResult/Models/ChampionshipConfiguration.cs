using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models
{
    public class ChampionshipConfiguration
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public bool IsExpired => DateOnly.FromDateTime(DateTime.UtcNow) > EndDate;

        public bool OverlapsWith(ChampionshipConfiguration other)
        {
            if (other == null) return false;
            if (Id == other.Id) return false;

            // Check if periods overlap
            return StartDate < other.EndDate && EndDate > other.StartDate;
        }

        public bool ContainsDate(DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            return dateOnly >= StartDate && dateOnly <= EndDate;
        }

        public (bool isValid, string? errorMessage) Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return (false, "Championship name is required");
            }

            if (StartDate > EndDate)
            {
                return (false, "Start date must be before end date");
            }

            return (true, null);
        }
    }
}
