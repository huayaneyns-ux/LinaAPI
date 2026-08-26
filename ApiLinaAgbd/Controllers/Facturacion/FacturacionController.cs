using ApiLinaAgbd.Models.Facturacion.Boleta;
using ApiLinaAgbd.Models.Facturacion.Factura;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Services.Facturacion;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion
{
	[ApiController]
	[Route("api/facturacion")]
	public class FacturacionController : ControllerBase
	{
		private readonly BoletaService _boletaService;
		private readonly FacturaService _facturaService;
		private readonly NotaDebitoService _notaDebitoService;

		public FacturacionController(
			BoletaService boletaService,
			FacturaService facturaService,
			NotaDebitoService notaDebitoService)
		{
			_boletaService = boletaService;
			_facturaService = facturaService;
			_notaDebitoService = notaDebitoService;
		}

		[HttpPost("boleta")]
		public async Task<IActionResult> EnviarBoleta([FromBody] BoletaRequestDto request)
		{
			var resultado = await _boletaService.Enviar(request);
			return ResponderEnvio(resultado);
		}

		[HttpPost("factura")]
		public async Task<IActionResult> EnviarFactura([FromBody] FacturaRequestDto request)
		{
			var resultado = await _facturaService.Enviar(request);
			return ResponderEnvio(resultado);
		}

		[HttpPost("nota-debito")]
		public async Task<IActionResult> EnviarNotaDebito([FromBody] NotaDebitoRequestDto request)
		{
			var resultado = await _notaDebitoService.Enviar(request);
			return ResponderEnvio(resultado);
		}

		private IActionResult ResponderEnvio(Models.Facturacion.FacturacionEnvioResultado resultado)
		{
			if (resultado.StatusCode >= 200 && resultado.StatusCode < 300)
			{
				return Ok(resultado);
			}

			if (resultado.StatusCode > 0)
			{
				return StatusCode(resultado.StatusCode, resultado);
			}

			return StatusCode(StatusCodes.Status502BadGateway, resultado);
		}
	}
}
