using R3EServerRaceResult.Settings;
using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.SimResult
{
    public class Config
    {
        public Config() { }

        public Config(ChampionshipAppSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.EventName)) Event = settings.EventName;
            if (!string.IsNullOrWhiteSpace(settings.EventUrl)) EventUrl = settings.EventUrl;
            if (!string.IsNullOrWhiteSpace(settings.LogoUrl)) LogoUrl = settings.LogoUrl;
            if (!string.IsNullOrWhiteSpace(settings.LeagueName)) League = settings.LeagueName;
            if (!string.IsNullOrWhiteSpace(settings.LeaugeUrl)) LeagueUrl = settings.LeaugeUrl;
            if (settings.PointSystem.Race.Count > 0) Points = string.Join(',', settings.PointSystem.Race);
            if (settings.PointSystem.Qualify.Count > 0) QPoints = string.Join(',', settings.PointSystem.Qualify);
            if (settings.PointSystem.BestLap > 0) BestLapBoints = settings.PointSystem.BestLap.ToString();
        }

        /// <summary>
        /// Logo image URL. Note: Must be a valid https URL.
        /// </summary>
        [JsonPropertyName("logo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Logo { get; set; }

        /// <summary>
        /// Logo link.
        /// </summary>
        [JsonPropertyName("logo_link")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// League name.
        /// </summary>
        [JsonPropertyName("league")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? League { get; set; }

        /// <summary>
        /// League link.
        /// </summary>
        [JsonPropertyName("leauge_link")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LeagueUrl { get; set; }

        /// <summary>
        /// Event name.
        /// </summary>
        [JsonPropertyName("event")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Event { get; set; }

        /// <summary>
        /// Event link.
        /// </summary>
        [JsonPropertyName("event_link")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? EventUrl { get; set; }

        /// <summary>
        /// Qualify points system. Note: Use the | separator for different points per race number. e.g.:2,1
        /// </summary>
        [JsonPropertyName(name: "q_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QPoints { get; set; }

        [JsonPropertyName("q_points_by_class")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QPointsByClass { get; set; }

        /// <summary>
        /// Race points system. Note: Use the | separator for different points per race number. e.g.:25,18,15,12,10,8,6,4,2,1
        /// </summary>
        [JsonPropertyName("points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Points { get; set; }

        [JsonPropertyName("points_by_class")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PointsByClass { get; set; }

        /// <summary>
        /// Race best lap points.
        /// </summary>
        [JsonPropertyName("best_lap_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BestLapBoints { get; set; }

        /// <summary>
        /// Race Stop/Go penalty will lose points.
        /// </summary>
        [JsonPropertyName("stopgo_lose_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StopGoPointLose { get; set; }

        /// <summary>
        /// Race Drive-through penalty will lose points.
        /// </summary>
        [JsonPropertyName("drivethrough_lose_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DriveThroughPointLose { get; set; }

        /// <summary>
        /// Race DNF will lose points.
        /// </summary>
        [JsonPropertyName("dnf_lose_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DnfPointLose { get; set; }

        /// <summary>
        /// Race DQ will lose points.
        /// </summary>
        [JsonPropertyName("dq_lose_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DqPointLose { get; set; }

        /// <summary>
        /// DNF/DQ drives will receive 0 race points.
        /// </summary>
        [JsonPropertyName("dnf_no_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? DnfNoPoints { get; set; } = true;

        /// <summary>
        /// The following driver positions will not lose points on DNF/DQ: Note: Use the | separator for multiple races. Each race number needs its own positions. Useful for disconnects and such.
        /// </summary>
        [JsonPropertyName("dnf_ignore_losing_points")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DnfIgnoreLosingPoints { get; set; }

        /// <summary>
        /// Discourage search engines from indexing this result.
        /// </summary>
        [JsonPropertyName("no_indexing")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NoIndexing { get; set; }

        /// <summary>
        /// Hide this result from overviews on simresults.net website.
        /// </summary>
        [JsonPropertyName("hide_from_overviews")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HideFromOverview { get; set; }

        /// <summary>
        /// Shorten surnames. Respect the privacy of the other drivers!
        /// </summary>
        [JsonPropertyName("shorten_lastnames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ShortenLastnames { get; set; } = true;

        /// <summary>
        /// Shorten firstnames.
        /// </summary>
        [JsonPropertyName("shorten_firstnames")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ShortenFirstnames { get; set; }

        /// <summary>
        /// Force showing the class and team columns on result summary.
        /// </summary>
        [JsonPropertyName("team")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Team { get; set; }

        /// <summary>
        /// Hide aids.
        /// </summary>
        [JsonPropertyName("hide_aids")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HideAids { get; set; }

        public void PatchWith(Config patchConfig)
        {
            ArgumentNullException.ThrowIfNull(patchConfig);

            foreach (var property in typeof(Config).GetProperties())
            {
                var newValue = property.GetValue(patchConfig);
                if (newValue != null)
                {
                    property.SetValue(this, newValue);
                }
            }
        }
    }
}
