using HtmlAgilityPack;
using SteamChartsAPI.Models;

namespace SteamChartsAPI.Services
{
    public class SteamChartsScraper
    {
        private readonly string? _baseUrl;
        private readonly string? _userAgent;
        private readonly string? _backendUrl;
        private readonly HttpClient _httpClient;

        public SteamChartsScraper(IConfiguration configuration, HttpClient httpClient)
        {
            _baseUrl = configuration["SteamCharts:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "SteamCharts BaseUrl não configurado.");
            _userAgent = configuration["SteamCharts:UserAgent"]
                         ?? throw new ArgumentNullException(nameof(_userAgent), "SteamCharts UserAgent não configurado.");
            _backendUrl = $"{configuration["StorageBackend:BaseUrl"]}{configuration["StorageBackend:GamesEndpoint"]}"
                          ?? throw new ArgumentNullException(nameof(_backendUrl), "Storage Backend URL não configurado.");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient não configurado.");
        }

        public async Task<List<Game>> GetTopGamesAsync()
        {
            var games = await FetchGamesAsync(_baseUrl);

            return games;
        }

        public async Task CollectHourlyPlayerDataAsync()
        {
            var games = await GetTopGamesAsync();

            if (games.Any())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(_backendUrl, games);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Erro ao enviar dados: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao enviar dados: {ex.Message}");
                }
            }
        }

        private async Task<List<Game>> FetchGamesAsync(string? url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), "A URL base não foi configurada.");

            var web = new HtmlWeb
            {
                UserAgent = _userAgent
            };

            var doc = await web.LoadFromWebAsync(url);

            if (doc.DocumentNode == null || doc.DocumentNode.InnerHtml.Contains("Access denied"))
                return new List<Game>();

            var games = new List<Game>();
            var rows = doc.DocumentNode.SelectNodes("//table[@id='top-games']/tbody/tr");

            if (rows == null)
                return new List<Game>();

            foreach (var row in rows.Take(10))
            {
                var nameNode = row.SelectSingleNode(".//td[@class='game-name left']/a");
                var currentPlayersNode = row.SelectSingleNode(".//td[@class='num']");
                var peakPlayersNode = row.SelectSingleNode(".//td[@class='num period-col peak-concurrent']");

                if (nameNode != null && currentPlayersNode != null && peakPlayersNode != null)
                {
                    var name = nameNode.InnerText.Trim();
                    var currentPlayers = currentPlayersNode.InnerText.Trim();
                    var peakPlayers = peakPlayersNode.InnerText.Trim();

                    games.Add(new Game
                    {
                        Name = name,
                        CurrentPlayers = int.Parse(currentPlayers.Replace(",", "")),
                        PeakPlayers = int.Parse(peakPlayers.Replace(",", "")),
                        Date = DateTime.UtcNow
                    });
                }
            }

            return games;
        }
    }
}