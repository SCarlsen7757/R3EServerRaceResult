using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.SimResult
{
    public class Config
    {
        /// <summary>
        /// Logo image URL. Note: Must be a valid https URL.
        /// </summary>
        [JsonPropertyName("logo")]
        public string Logo { get; set; } = string.Empty;

        /// <summary>
        /// Logo link.
        /// </summary>
        [JsonPropertyName("logo_link")]
        public string LogoUrl { get; set; } = string.Empty;

        /// <summary>
        /// League name.
        /// </summary>
        [JsonPropertyName("league")]
        public string League { get; set; } = string.Empty;

        /// <summary>
        /// League link.
        /// </summary>
        [JsonPropertyName("leauge_link")]
        public string LeagueUrl { get; set; } = string.Empty;

        /// <summary>
        /// Event name.
        /// </summary>
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// Event link.
        /// </summary>
        [JsonPropertyName("event_link")]
        public string EventUrl { get; set; } = string.Empty;

        /// <summary>
        /// Qualify points system. Note: Use the | separator for different points per race number. e.g.:2,1
        /// </summary>
        [JsonPropertyName(name: "q_points")]
        public string QPoints { get; set; } = string.Empty;

        [JsonPropertyName("q_points_by_class")]
        public string QPointsByClass { get; set; } = string.Empty;

        /// <summary>
        /// Race points system. Note: Use the | separator for different points per race number. e.g.:25,18,15,12,10,8,6,4,2,1
        /// </summary>
        [JsonPropertyName("points")]
        public string Points { get; set; } = string.Empty;

        [JsonPropertyName("points_by_class")]
        public string PointsByClass { get; set; } = string.Empty;

        /// <summary>
        /// Race best lap points.
        /// </summary>
        [JsonPropertyName("best_lap_points")]
        public string BestLapBoints { get; set; } = string.Empty;

        /// <summary>
        /// Race Stop/Go penalty will lose points.
        /// </summary>
        [JsonPropertyName("stopgo_lose_points")]
        public string StopGoPointLose { get; set; } = string.Empty;

        /// <summary>
        /// Race Drive-through penalty will lose points.
        /// </summary>
        [JsonPropertyName("drivethrough_lose_points")]
        public string DriveThroughPointLose { get; set; } = string.Empty;

        /// <summary>
        /// Race DNF will lose points.
        /// </summary>
        [JsonPropertyName("dnf_lose_points")]
        public string DnfPointLose { get; set; } = string.Empty;

        /// <summary>
        /// Race DQ will lose points.
        /// </summary>
        [JsonPropertyName("dq_lose_points")]
        public string DqPointLose { get; set; } = string.Empty;

        /// <summary>
        /// DNF/DQ drives will receive 0 race points.
        /// </summary>
        [JsonPropertyName("dnf_no_points")]
        public bool DnfNoPoints { get; set; } = true;

        /// <summary>
        /// The following driver positions will not lose points on DNF/DQ: Note: Use the | separator for multiple races. Each race number needs its own positions. Useful for disconnects and such.
        /// </summary>
        [JsonPropertyName("dnf_ignore_losing_points")]
        public string DnfIgnoreLosingPoints { get; set; } = string.Empty;

        /// <summary>
        /// Discourage search engines from indexing this result.
        /// </summary>
        [JsonPropertyName("no_indexing")]
        public string NoIndexing { get; set; } = string.Empty;

        /// <summary>
        /// Hide this result from overviews on simresults.net website.
        /// </summary>
        [JsonPropertyName("hide_from_overviews")]
        public bool HideFromOverview { get; set; }

        /// <summary>
        /// Shorten surnames. Respect the privacy of the other drivers!
        /// </summary>
        [JsonPropertyName("shorten_lastnames")]
        public bool ShortenLastnames { get; set; } = true;

        /// <summary>
        /// Shorten firstnames.
        /// </summary>
        [JsonPropertyName("shorten_firstnames")]
        public bool ShortenFirstnames { get; set; }

        /// <summary>
        /// Force showing the class and team columns on result summary.
        /// </summary>
        [JsonPropertyName("team")]
        public bool Team { get; set; }

        /// <summary>
        /// Hide aids.
        /// </summary>
        [JsonPropertyName("hide_aids")]
        public bool HideAids { get; set; }
    }
}
