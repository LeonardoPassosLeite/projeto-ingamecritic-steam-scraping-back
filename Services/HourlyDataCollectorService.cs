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
        var now = DateTime.UtcNow;
        var nextRun = now.AddHours(1).Date.AddHours(now.Hour + 1);
        var delay = nextRun - now;

        _logger.LogInformation("Aguardando {DelayMinutes} minutos até a primeira execução às {NextRun}.", delay.TotalMinutes, nextRun);

        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("O serviço foi interrompido antes da primeira execução.");
            return;
        }

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

            now = DateTime.UtcNow;
            nextRun = now.AddHours(1).Date.AddHours(now.Hour + 1);
            delay = nextRun - now;

            try
            {
                _logger.LogInformation("Aguardando {DelayMinutes} minutos até a próxima execução às {NextRun}.", delay.TotalMinutes, nextRun);
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