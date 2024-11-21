using SteamChartsAPI.Services;

public class HourlyDataCollectorService : BackgroundService
{
    private readonly SteamChartsScraper _scraper;

    public HourlyDataCollectorService(SteamChartsScraper scraper)
    {
        _scraper = scraper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _scraper.CollectHourlyPlayerDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a coleta de dados: {ex.Message}");
            }

            var now = DateTime.UtcNow;
            var nextRun = now.AddHours(1).Date.AddHours(now.Hour + 1);
            var delay = nextRun - now;
            await Task.Delay(delay, stoppingToken);
        }
    }
}