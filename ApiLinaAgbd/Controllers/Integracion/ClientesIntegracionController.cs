using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Services.Integracion;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Integracion;

[ApiController]
[Route("api/v1")]
public class ClientesIntegracionController : ControllerBase
{
	private readonly IIntegracionClientesService _service;
	private readonly IntegracionService _integracionService;

	public ClientesIntegracionController(IIntegracionClientesService service, IntegracionService integracionService)
	{
		_service = service;
		_integracionService = integracionService;
	}

	[HttpGet("clientes")]
	public IActionResult ListarClientes()
	{
		var apiKey = Request.Headers["X-API-Key"].ToString();
		if (string.IsNullOrWhiteSpace(apiKey)) apiKey = Request.Headers["X-Integration-Key"].ToString();
		try { return Ok(_integracionService.Clientes(apiKey, HttpContext.Connection.RemoteIpAddress?.ToString())); }
		catch (UnauthorizedAccessException ex) { return Unauthorized(new { detail = ex.Message }); }
	}

	[HttpGet("integracion/clientes")]
	public IActionResult ListarClientesIntegracion() => ListarClientes();

	[HttpPost("webhooks/cliente-externo")]
	public IActionResult RegistrarClienteExterno([FromBody] ClienteWebhookDto cliente)
	{
		var inicio = DateTime.UtcNow;
		var integrationKey = Request.Headers["X-API-Key"].ToString();
		if (string.IsNullOrWhiteSpace(integrationKey)) integrationKey = Request.Headers["X-Integration-Key"].ToString();
		long auditoriaId;
		try
		{
			auditoriaId = _integracionService.IniciarAuditoria(integrationKey, "WEBHOOK_CLIENTE_EXTERNO", inicio, HttpContext.Connection.RemoteIpAddress?.ToString());
		}
		catch (UnauthorizedAccessException ex)
		{
			return Unauthorized(new { detail = ex.Message });
		}

		var camposFaltantes = new List<string>();
		if (string.IsNullOrWhiteSpace(cliente.nombre)) camposFaltantes.Add("nombre");
		if (string.IsNullOrWhiteSpace(cliente.documento)) camposFaltantes.Add("documento");
		if (string.IsNullOrWhiteSpace(cliente.telefono)) camposFaltantes.Add("telefono");
		if (string.IsNullOrWhiteSpace(cliente.email)) camposFaltantes.Add("email");

		if (camposFaltantes.Count > 0)
		{
			_integracionService.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "VALIDACION_ERROR", 0, string.Join(", ", camposFaltantes));
			return UnprocessableEntity(new
			{
				code = "CAMPOS_OBLIGATORIOS",
				detail = $"Los siguientes campos son obligatorios: {string.Join(", ", camposFaltantes)}."
			});
		}

		var resultado = _service.RegistrarClienteExterno(cliente, integrationKey);
		if (resultado.YaExistia)
		{
			_integracionService.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "YA_EXISTIA", 0, resultado.CampoDuplicado);
			var campo = resultado.CampoDuplicado == "correo" ? "correo electrónico" : "documento";
			return Conflict(new
			{
				code = "CLIENTE_YA_EXISTE",
				detail = $"Ya existe una cuenta con ese {campo}."
			});
		}

		_integracionService.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "EXITOSO", 1, null);
		return StatusCode(StatusCodes.Status201Created, resultado.Cliente);
	}
}
