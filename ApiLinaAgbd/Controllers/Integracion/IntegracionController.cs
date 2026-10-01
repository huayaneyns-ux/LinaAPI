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

    [HttpGet("empresas/{empresaId}/catalogo")]
    public IActionResult CatalogoEmpresa(int empresaId) => Ok(_service.CatalogoAdmin(empresaId));

    [HttpPut("empresas/{empresaId}/catalogo")]
    public IActionResult GuardarCatalogoEmpresa(int empresaId, [FromBody] IntegracionCatalogoSeleccionGuardarDto dto)
    {
        _service.GuardarSeleccionCatalogo(empresaId, dto);
        return Ok(_service.CatalogoAdmin(empresaId));
    }

    [HttpPut("empresas/{empresaId}")]
    public IActionResult ActualizarEmpresa(int empresaId, [FromBody] IntegracionConfiguracionGuardarDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreEmpresa)) return UnprocessableEntity(new { detail = "NombreEmpresa es obligatorio." });
        return Ok(_service.Guardar(dto, empresaId));
    }

    [HttpDelete("empresas/{empresaId}")]
    public IActionResult EliminarEmpresa(int empresaId)
    {
        try
        {
            _service.EliminarConfiguracion(empresaId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { detail = ex.Message }); }
    }

    [HttpPost("empresas/{empresaId}/consultar/{tipo}")]
    public async Task<IActionResult> ConsultarEmpresaExterna(int empresaId, string tipo)
    {
        if (!tipo.Equals("PRODUCTOS", StringComparison.OrdinalIgnoreCase) &&
            !tipo.Equals("PROVEEDORES", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { detail = "El tipo debe ser PRODUCTOS o PROVEEDORES." });
        try { return Ok(await _service.ConsultarEmpresaExternaAsync(empresaId, tipo, HttpContext.Connection.RemoteIpAddress?.ToString())); }
        catch (KeyNotFoundException ex) { return NotFound(new { detail = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { detail = ex.Message }); }
        catch (HttpRequestException ex) { return StatusCode(StatusCodes.Status502BadGateway, new { detail = ex.Message }); }
    }

}

[ApiController]
[Route("api/v1/integracion")]
public sealed class CatalogoIntegracionController : ControllerBase
{
    private readonly IntegracionService _service;
    public CatalogoIntegracionController(IntegracionService service) => _service = service;

    [HttpGet("catalogo")]
    [HttpGet("productos-proveedores")]
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

    [HttpGet("productos")]
    public IActionResult Productos()
    {
        var apiKey = Request.Headers["X-Integration-Key"].ToString();
        try
        {
            var result = _service.Catalogo(apiKey, HttpContext.Connection.RemoteIpAddress?.ToString(), "PRODUCTOS");
            return Ok(new { operacion = "CONSULTA_CATALOGO_PRODUCTOS", fecha = result.Inicio, productos = result.Productos });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { detail = ex.Message }); }
    }

    [HttpGet("proveedores")]
    public IActionResult Proveedores()
    {
        var apiKey = Request.Headers["X-Integration-Key"].ToString();
        try
        {
            var result = _service.Catalogo(apiKey, HttpContext.Connection.RemoteIpAddress?.ToString(), "PROVEEDORES");
            return Ok(new { operacion = "CONSULTA_CATALOGO_PROVEEDORES", fecha = result.Inicio, proveedores = result.Proveedores });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { detail = ex.Message }); }
    }

    [HttpPost("catalogo/confirmacion")]
    public IActionResult ConfirmarCatalogo([FromBody] IntegracionCatalogoConfirmacionDto dto)
    {
        var apiKey = Request.Headers["X-Integration-Key"].ToString();
        try { return Ok(new { confirmado = _service.ConfirmarCatalogo(apiKey, dto, HttpContext.Connection.RemoteIpAddress?.ToString()) }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { detail = ex.Message }); }
    }
}
