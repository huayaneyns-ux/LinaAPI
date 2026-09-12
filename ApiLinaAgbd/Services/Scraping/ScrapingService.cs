using ApiLinaAgbd.Models.Scraping;
using ApiLinaAgbd.Repositories.Scraping;

namespace ApiLinaAgbd.Services.Scraping;

public class ScrapingService : IScrapingService
{
    private readonly IScrapingRepository _repository;
    public ScrapingService(IScrapingRepository repository) => _repository = repository;
    public List<ScrapingMatchDto> ListarMatches() => _repository.ListarMatches();
    public void ActualizarDecision(long matchId, ScrapingDecisionDto decision) => _repository.ActualizarDecision(matchId, decision);
}
