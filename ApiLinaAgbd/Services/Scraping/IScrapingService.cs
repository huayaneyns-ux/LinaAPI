using ApiLinaAgbd.Models.Scraping;

namespace ApiLinaAgbd.Services.Scraping;

public interface IScrapingService
{
    List<ScrapingMatchDto> ListarMatches();
    List<ScrapedProductOptionDto> ListarProductosScrapeados();
    void ActualizarDecision(long matchId, ScrapingDecisionDto decision);
    void CrearMatchManual(ScrapingManualMatchDto match);
}
