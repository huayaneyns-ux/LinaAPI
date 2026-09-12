using ApiLinaAgbd.Models.Scraping;

namespace ApiLinaAgbd.Repositories.Scraping;

public interface IScrapingRepository
{
    List<ScrapingMatchDto> ListarMatches();
    void ActualizarDecision(long matchId, ScrapingDecisionDto decision);
}
