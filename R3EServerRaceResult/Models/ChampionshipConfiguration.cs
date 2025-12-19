namespace R3EServerRaceResult.Models
{
    public class ChampionshipConfiguration
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                return today >= StartDate && today <= EndDate;
            }
        }

        public bool IsExpired => DateOnly.FromDateTime(DateTime.UtcNow) > EndDate;

        public bool OverlapsWith(ChampionshipConfiguration other)
        {
            if (other == null) return false;
            if (Id == other.Id) return false;

            // Check if periods overlap (inclusive of boundary dates)
            return StartDate <= other.EndDate && EndDate >= other.StartDate;
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
