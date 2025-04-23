namespace R3EServerRaceResult.Settings
{
    public class ChampionshipAppSettings
    {
        public string WebServer { get; set; } = string.Empty;
        public string EventName { get; set; } = "Championship";
        public string EventUrl { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string LeagueName { get; set; } = string.Empty;
        public string LeaugeUrl { get; set; } = string.Empty;
        public PointSystem PointSystem { get; set; } = new();
    }
}
