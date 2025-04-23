namespace R3EServerRaceResult.Settings
{
    public class PointSystem
    {
        public List<int> Race { get; set; } = [25, 18, 15, 12, 10, 8, 6, 4, 2, 1];
        public List<int> Qualify { get; set; } = [];
        public int BestLap { get; set; }
    }
}
