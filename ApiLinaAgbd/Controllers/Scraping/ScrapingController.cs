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

    [HttpGet("products")]
    public IActionResult ListarProductosScrapeados() => Ok(_service.ListarProductosScrapeados());

    [HttpPost("matches/manual")]
    public IActionResult CrearMatchManual([FromBody] ScrapingManualMatchDto match)
    {
        try { _service.CrearMatchManual(match); return Ok(new { mensaje = "Producto relacionado correctamente." }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { mensaje = ex.Message }); }
    }

    [HttpPut("matches/{id:long}/decision")]
    public IActionResult ActualizarDecision(long id, [FromBody] ScrapingDecisionDto decision)
    {
        try { _service.ActualizarDecision(id, decision); return Ok(new { mensaje = "Decisión actualizada correctamente." }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }
}
