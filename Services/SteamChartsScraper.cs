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
        private readonly ILogger<SteamChartsScraper> _logger;

        public SteamChartsScraper(IConfiguration configuration, HttpClient httpClient, ILogger<SteamChartsScraper> logger)
        {
            _baseUrl = configuration["SteamCharts:BaseUrl"]
                       ?? throw new ArgumentNullException(nameof(_baseUrl), "SteamCharts BaseUrl não configurado.");
            _userAgent = configuration["SteamCharts:UserAgent"]
                         ?? throw new ArgumentNullException(nameof(_userAgent), "SteamCharts UserAgent não configurado.");
            _backendUrl = $"{configuration["StorageBackend:BaseUrl"]}{configuration["StorageBackend:GamesEndpoint"]}"
                          ?? throw new ArgumentNullException(nameof(_backendUrl), "Storage Backend URL não configurado.");
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient não configurado.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task CollectHourlyPlayerDataAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando a coleta de dados.");
                var games = await GetTopGamesAsync();

                if (games.Any())
                {
                    await SendDataToBackendAsync(games);
                    _logger.LogInformation("Coleta de dados e envio ao backend concluídos com sucesso.");
                }
                else
                {
                    _logger.LogWarning("Nenhum jogo encontrado para enviar.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado na coleta de dados.");
            }
        }

        public async Task<List<Game>> GetTopGamesAsync()
        {
            try
            {
                return await FetchGamesAsync(_baseUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de rede ao acessar {_BaseUrl}.", _baseUrl);
                return new List<Game>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter os jogos.");
                return new List<Game>();
            }
        }

        private async Task<List<Game>> FetchGamesAsync(string? url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), "A URL base não foi configurada.");

            try
            {
                _logger.LogInformation("Carregando dados do HTML da URL: {Url}", url);
                var web = new HtmlWeb { UserAgent = _userAgent };
                var doc = await web.LoadFromWebAsync(url);

                if (doc.DocumentNode == null || doc.DocumentNode.InnerHtml.Contains("Access denied"))
                {
                    _logger.LogWarning("Acesso negado ou HTML inválido ao acessar: {Url}", url);
                    return new List<Game>();
                }

                return ParseGames(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar ou processar HTML.");
                return new List<Game>();
            }
        }

        private List<Game> ParseGames(HtmlDocument doc)
        {
            var games = new List<Game>();
            var rows = doc.DocumentNode.SelectNodes("//table[@id='top-games']/tbody/tr");

            if (rows == null)
            {
                _logger.LogWarning("Nenhuma tabela de jogos encontrada no HTML.");
                return games;
            }

            foreach (var row in rows.Take(10))
            {
                try
                {
                    var nameNode = row.SelectSingleNode(".//td[@class='game-name left']/a");
                    var currentPlayersNode = row.SelectSingleNode(".//td[@class='num']");
                    var peakPlayersNode = row.SelectSingleNode(".//td[@class='num period-col peak-concurrent']");

                    if (nameNode != null && currentPlayersNode != null && peakPlayersNode != null)
                    {
                        var name = nameNode.InnerText.Trim();
                        var currentPlayers = int.Parse(currentPlayersNode.InnerText.Replace(",", "").Trim());
                        var peakPlayers = int.Parse(peakPlayersNode.InnerText.Replace(",", "").Trim());

                        games.Add(new Game
                        {
                            Name = name,
                            CurrentPlayers = currentPlayers,
                            PeakPlayers = peakPlayers,
                            Date = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar uma linha da tabela.");
                }
            }

            return games;
        }

        private async Task SendDataToBackendAsync(List<Game> games)
        {
            try
            {
                _logger.LogInformation("Enviando dados ao backend: {BackendUrl}", _backendUrl);
                var response = await _httpClient.PostAsJsonAsync(_backendUrl, games);

                if (!response.IsSuccessStatusCode)
                    _logger.LogError("Erro ao enviar dados para o backend: {StatusCode}", response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de rede ao enviar dados ao backend.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar dados ao backend.");
            }
        }
    }
}