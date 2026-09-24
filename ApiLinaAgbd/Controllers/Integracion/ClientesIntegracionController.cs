using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Services.Integracion;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Integracion;

[ApiController]
[Route("api/v1")]
public class ClientesIntegracionController : ControllerBase
{
	private readonly IIntegracionClientesService _service;

	public ClientesIntegracionController(IIntegracionClientesService service) => _service = service;

	[HttpGet("clientes")]
	public IActionResult ListarClientes() => Ok(_service.ListarClientes());

	[HttpPost("webhooks/cliente-externo")]
	public IActionResult RegistrarClienteExterno([FromBody] ClienteWebhookDto cliente)
	{
		if (string.IsNullOrWhiteSpace(cliente.nombre) || string.IsNullOrWhiteSpace(cliente.origen))
		{
			return UnprocessableEntity(new { detail = "nombre y origen son obligatorios" });
		}

		var resultado = _service.RegistrarClienteExterno(cliente);
		if (resultado.YaExistia)
		{
			var campo = resultado.CampoDuplicado == "correo" ? "correo electrónico" : "documento";
			return Conflict(new
			{
				code = "CLIENTE_YA_EXISTE",
				detail = $"Ya existe una cuenta con ese {campo}."
			});
		}

		return StatusCode(StatusCodes.Status201Created, resultado.Cliente);
	}
}
