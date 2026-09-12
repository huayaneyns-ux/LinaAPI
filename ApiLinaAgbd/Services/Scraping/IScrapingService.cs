using ApiLinaAgbd.Models.Scraping;

namespace ApiLinaAgbd.Services.Scraping;

public interface IScrapingService
{
    List<ScrapingMatchDto> ListarMatches();
    void ActualizarDecision(long matchId, ScrapingDecisionDto decision);
}
