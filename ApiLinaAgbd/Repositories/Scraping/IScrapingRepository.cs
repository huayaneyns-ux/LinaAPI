using ApiLinaAgbd.Models.Scraping;

namespace ApiLinaAgbd.Repositories.Scraping;

public interface IScrapingRepository
{
    List<ScrapingMatchDto> ListarMatches();
    List<ScrapedProductOptionDto> ListarProductosScrapeados();
    void ActualizarDecision(long matchId, ScrapingDecisionDto decision);
    void CrearMatchManual(ScrapingManualMatchDto match);
}
