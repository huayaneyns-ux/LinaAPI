using ApiLinaAgbd.Models.Facturacion.NotaCredito;
using ApiLinaAgbd.Services.Facturacion.NotaCredito;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion.NotaCredito
{
	[ApiController]
	[Route("api/facturacion")]
	[Tags("Facturacion - Nota Credito (NC)")]
	public class NotaCreditoController : ControllerBase
	{
		private readonly INotaCreditoService _notaCreditoService;

		public NotaCreditoController(INotaCreditoService notaCreditoService)
		{
			_notaCreditoService = notaCreditoService;
		}

		[HttpGet("comprobantes/notas/bases")]
		public async Task<IActionResult> ListarComprobantesBaseParaNotas()
		{
			var comprobantes = await _notaCreditoService.ListarComprobantesBaseAsync();
			return Ok(comprobantes);
		}

		[HttpPost("comprobantes/notas/credito")]
		public async Task<IActionResult> EmitirNotaCredito([FromBody] NotaCreditoEmitirRequestDto request)
		{
			try
			{
				var resultado = await _notaCreditoService.EmitirAsync(request);
				return Ok(resultado);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}
	}
}
