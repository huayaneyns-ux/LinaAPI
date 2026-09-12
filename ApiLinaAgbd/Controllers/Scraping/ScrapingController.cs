using ApiLinaAgbd.Models.Scraping;
using ApiLinaAgbd.Services.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Scraping;

[ApiController]
[Route("api/[controller]")]
public class ScrapingController : ControllerBase
{
    private readonly IScrapingService _service;
    public ScrapingController(IScrapingService service) => _service = service;

    [HttpGet("matches")]
    public IActionResult ListarMatches() => Ok(_service.ListarMatches());

    [HttpPut("matches/{id:long}/decision")]
    public IActionResult ActualizarDecision(long id, [FromBody] ScrapingDecisionDto decision)
    {
        try { _service.ActualizarDecision(id, decision); return Ok(new { mensaje = "Decisión actualizada correctamente." }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }
}
