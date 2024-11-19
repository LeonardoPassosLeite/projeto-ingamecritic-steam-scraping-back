using Microsoft.AspNetCore.Mvc;
using SteamChartsAPI.Services;
using SteamChartsAPI.Models;

namespace SteamChartsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SteamChartsController : ControllerBase
    {
        private readonly SteamChartsScraper _scraper;

        public SteamChartsController(SteamChartsScraper scraper)
        {
            _scraper = scraper;
        }

        [HttpGet("top-games")]
        public async Task<ActionResult<List<Game>>> GetTopGames()
        {
            try
            {
                var games = await _scraper.GetTopGamesAsync();
                if (games.Count == 0)
                {
                    return NotFound("Nenhum jogo encontrado no SteamCharts.");
                }
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao obter dados: {ex.Message}");
            }
        }
    }
}