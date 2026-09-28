using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Services.Integracion;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Integracion;

[ApiController]
[Route("api/[controller]")]
public sealed class IntegracionController : ControllerBase
{
    private readonly IntegracionService _service;
    public IntegracionController(IntegracionService service) => _service = service;

    [HttpGet("configuracion")]
    public IActionResult Configuracion() => Ok(_service.Obtener());

    [HttpGet("empresas")]
    public IActionResult Empresas() => Ok(_service.Listar());

    [HttpPost("empresas")]
    public IActionResult CrearEmpresa([FromBody] IntegracionConfiguracionGuardarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreEmpresa)) return UnprocessableEntity(new { detail = "NombreEmpresa es obligatorio." });
        var id = _service.Guardar(dto);
        return Created($"/api/Integracion/empresas/{id}", _service.Obtener(id));
    }

    [HttpPut("configuracion")]
    public IActionResult Guardar([FromBody] IntegracionConfiguracionGuardarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreEmpresa)) return UnprocessableEntity(new { detail = "NombreEmpresa es obligatorio." });
        return Ok(_service.Guardar(dto, _service.Obtener()?.Id));
    }

    [HttpGet("auditoria")]
    public IActionResult Auditoria() => Ok(_service.Auditoria());

}

[ApiController]
[Route("api/v1/integracion")]
public sealed class CatalogoIntegracionController : ControllerBase
{
    private readonly IntegracionService _service;
    public CatalogoIntegracionController(IntegracionService service) => _service = service;

    [HttpGet("catalogo")]
    public IActionResult Catalogo()
    {
        var apiKey = Request.Headers["X-Integration-Key"].ToString();
        try
        {
            var result = _service.Catalogo(apiKey, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { operacion = "CONSULTA_CATALOGO", fecha = result.Inicio, productos = result.Productos, proveedores = result.Proveedores });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { detail = ex.Message }); }
    }
}
