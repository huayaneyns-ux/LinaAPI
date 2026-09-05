using ApiLinaAgbd.Models.ApiPeru;
using ApiLinaAgbd.Services.Persona;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Persona
{
	[ApiController]
	[Route("api/[controller]")]
	public class PersonaController : ControllerBase
	{
		private readonly IApiPeruService _apiPeruService;

		public PersonaController(IApiPeruService apiPeruService)
		{
			_apiPeruService = apiPeruService;
		}

		/// <summary>
		/// Consulta persona/empresa por tipo de documento (DNI o RUC) y número mediante POST.
		/// </summary>
		[HttpPost("consultar")]
		public async Task<IActionResult> Consultar([FromBody] ConsultaPersonaRequestDto request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.TipoDocumento) || string.IsNullOrWhiteSpace(request.Numero))
			{
				return BadRequest(new PersonaResponseDto
				{
					Success = false,
					Mensaje = "Debe proporcionar tipoDocumento ('DNI' o 'RUC') y el número de documento."
				});
			}

			var resultado = await _apiPeruService.ConsultarYRegistrarPersonaAsync(request.TipoDocumento, request.Numero);
			if (!resultado.Success)
			{
				return NotFound(resultado);
			}

			return Ok(resultado);
		}

		/// <summary>
		/// Consulta persona/empresa por Query Params (?tipoDocumento=DNI&numero=12345678)
		/// </summary>
		[HttpGet("consultar")]
		public async Task<IActionResult> ConsultarPorQuery([FromQuery] string tipoDocumento, [FromQuery] string numero)
		{
			if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numero))
			{
				return BadRequest(new PersonaResponseDto
				{
					Success = false,
					Mensaje = "Debe proporcionar tipoDocumento y numero como parámetros."
				});
			}

			var resultado = await _apiPeruService.ConsultarYRegistrarPersonaAsync(tipoDocumento, numero);
			if (!resultado.Success)
			{
				return NotFound(resultado);
			}

			return Ok(resultado);
		}

		/// <summary>
		/// Consulta persona/empresa por ruta (/api/persona/consultar/DNI/12345678)
		/// </summary>
		[HttpGet("consultar/{tipoDocumento}/{numero}")]
		public async Task<IActionResult> ConsultarPorRuta(string tipoDocumento, string numero)
		{
			var resultado = await _apiPeruService.ConsultarYRegistrarPersonaAsync(tipoDocumento, numero);
			if (!resultado.Success)
			{
				return NotFound(resultado);
			}

			return Ok(resultado);
		}

		/// <summary>
		/// Atajo directo para consultar DNI
		/// </summary>
		[HttpGet("dni/{dni}")]
		public async Task<IActionResult> ConsultarDni(string dni)
		{
			var resultado = await _apiPeruService.ConsultarYRegistrarPersonaAsync("DNI", dni);
			if (!resultado.Success)
			{
				return NotFound(resultado);
			}

			return Ok(resultado);
		}

		/// <summary>
		/// Atajo directo para consultar RUC
		/// </summary>
		[HttpGet("ruc/{ruc}")]
		public async Task<IActionResult> ConsultarRuc(string ruc)
		{
			var resultado = await _apiPeruService.ConsultarYRegistrarPersonaAsync("RUC", ruc);
			if (!resultado.Success)
			{
				return NotFound(resultado);
			}

			return Ok(resultado);
		}
	}
}
