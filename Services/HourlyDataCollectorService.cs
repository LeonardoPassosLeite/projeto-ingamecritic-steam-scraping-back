using SteamChartsAPI.Services;
using SteamChartsAPI.Exceptions;

public class HourlyDataCollectorService : BackgroundService
{
    private readonly SteamChartsScraper _scraper;
    private readonly ILogger<HourlyDataCollectorService> _logger;

    public HourlyDataCollectorService(SteamChartsScraper scraper, ILogger<HourlyDataCollectorService> logger)
    {
        _scraper = scraper ?? throw new ArgumentNullException(nameof(scraper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço iniciado. Executando coleta de dados a cada 1 minuto para fins de teste.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Iniciando coleta de dados de jogadores...");
                await _scraper.CollectHourlyPlayerDataAsync();
                _logger.LogInformation("Coleta de dados concluída com sucesso.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("O serviço foi interrompido.");
                break;
            }
            catch (ScraperException ex)
            {
                _logger.LogWarning(ex, "Erro na coleta de dados do scraper: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante a coleta de dados.");
            }

            // Aguarda 1 minuto antes de executar novamente
            var delay = TimeSpan.FromMinutes(1);

            try
            {
                _logger.LogInformation("Aguardando 1 minuto para a próxima execução.");
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("O serviço foi interrompido durante o tempo de espera.");
                break;
            }
        }
    }
}
