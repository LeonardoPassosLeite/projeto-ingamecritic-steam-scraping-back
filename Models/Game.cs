namespace SteamChartsAPI.Models
{
    public class Game
    {
        public string Name { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int PeakPlayers { get; set; }
    }
}
