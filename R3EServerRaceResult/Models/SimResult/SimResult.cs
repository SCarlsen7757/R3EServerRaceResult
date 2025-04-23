using System.Text.Json.Serialization;

namespace R3EServerRaceResult.Models.SimResult
{
    public class SimResult
    {
        public SimResult() { }

        public SimResult(Settings.ChampionshipAppSettings appSettings)
        {
            Config.LogoUrl = appSettings.LogoUrl;
            Config.Event = appSettings.EventName;
            Config.EventUrl = appSettings.EventUrl;
            Config.League = appSettings.LeagueName;
            Config.LeagueUrl = appSettings.LeaugeUrl;
            Config.Points = string.Join(',', appSettings.PointSystem.Race);
            Config.QPoints = string.Join(',', appSettings.PointSystem.Qualify);
            Config.BestLapBoints = appSettings.PointSystem.BestLap.ToString();
        }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("results")]
        public List<Result> Results { get; set; } = [];

        [JsonPropertyName("config")]
        public Config Config { get; set; } = new();
    }
}
