using HtmlAgilityPack;
using SteamChartsAPI.Models;

namespace SteamChartsAPI.Services
{
    public class SteamChartsScraper
    {
        private readonly string? _baseUrl;
        private readonly string? _userAgent;

        public SteamChartsScraper(IConfiguration configuration)
        {
            _baseUrl = configuration["SteamCharts:BaseUrl"];
            _userAgent = configuration["SteamCharts:UserAgent"];
        }

        public async Task<List<Game>> GetTopGamesAsync()
        {
            var web = new HtmlWeb
            {
                UserAgent = _userAgent
            };

            var doc = await web.LoadFromWebAsync(_baseUrl);

            if (doc.DocumentNode == null || doc.DocumentNode.InnerHtml.Contains("Access denied"))
            {
                Console.WriteLine("Acesso negado ou conteúdo vazio no SteamCharts.");
                return new List<Game>();
            }

            var games = new List<Game>();
            var rows = doc.DocumentNode.SelectNodes("//table[@id='top-games']/tbody/tr");

            if (rows == null)
            {
                Console.WriteLine("Tabela de jogos não encontrada no SteamCharts.");
                return new List<Game>();
            }

            foreach (var row in rows.Take(5))
            {
                var name = row.SelectSingleNode(".//td[@class='game-name left']/a")?.InnerText.Trim();
                var currentPlayers = row.SelectSingleNode(".//td[@class='num']")?.InnerText.Trim();
                var peakPlayers = row.SelectSingleNode(".//td[@class='num period-col peak-concurrent']")?.InnerText.Trim();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(currentPlayers) && !string.IsNullOrEmpty(peakPlayers))
                {
                    games.Add(new Game
                    {
                        Name = name,
                        CurrentPlayers = int.Parse(currentPlayers.Replace(",", "")),
                        PeakPlayers = int.Parse(peakPlayers.Replace(",", ""))
                    });
                }
            }

            return games;
        }
    }
}