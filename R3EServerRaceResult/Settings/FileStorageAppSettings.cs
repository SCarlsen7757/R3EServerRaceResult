namespace R3EServerRaceResult.Settings
{
    public class FileStorageAppSettings
    {
        public string MountedVolumePath { get; set; } = "/app/data";
        public string ResultFileName { get; set; } = "summary";
        public GroupingStrategyType GroupingStrategy { get; set; } = GroupingStrategyType.Monthly;
        public int RacesPerChampionship { get; set; } = 4;
        public string DatabaseConnectionString { get; set; } = string.Empty;
    }
}