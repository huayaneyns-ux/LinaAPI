using ApiLinaAgbd.Models.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Services.Facturacion.LiquidacionCompra;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion.LiquidacionCompra
{
	[ApiController]
	[Route("api/facturacion")]
	[Tags("Facturacion - Liquidacion Compra (04)")]
	public class LiquidacionCompraController : ControllerBase
	{
		private readonly ILiquidacionCompraService _liquidacionCompraService;

		public LiquidacionCompraController(ILiquidacionCompraService liquidacionCompraService)
		{
			_liquidacionCompraService = liquidacionCompraService;
		}

		[HttpGet("comprobantes/liquidaciones/compras-disponibles")]
		public async Task<IActionResult> ListarComprasDisponiblesParaLiquidacion()
		{
			var compras = await _liquidacionCompraService.ListarComprasDisponiblesAsync();
			return Ok(compras);
		}

		[HttpPost("comprobantes/liquidaciones")]
		public async Task<IActionResult> EmitirLiquidacionCompra([FromBody] LiquidacionCompraEmitirRequestDto request)
		{
			try
			{
				var resultado = await _liquidacionCompraService.EmitirAsync(request);
				return Ok(resultado);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}
	}
}
