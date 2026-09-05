using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Services.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Services.Facturacion.Documentos;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion.ComprobantesVenta
{
	[ApiController]
	[Route("api/facturacion")]
	[Tags("Facturacion - Factura / Boleta (01 / 03)")]
	public class ComprobantesVentaController : ControllerBase
	{
		private readonly IComprobanteVentasService _comprobanteVentasService;
		private readonly IDocumentoFacturacionService _documentoFacturacionService;

		public ComprobantesVentaController(
			IComprobanteVentasService comprobanteVentasService,
			IDocumentoFacturacionService documentoFacturacionService)
		{
			_comprobanteVentasService = comprobanteVentasService;
			_documentoFacturacionService = documentoFacturacionService;
		}

		[HttpGet("comprobantes/ventas-disponibles")]
		public async Task<IActionResult> ListarVentasDisponibles()
		{
			var ventas = await _comprobanteVentasService.ListarVentasDisponiblesAsync();
			return Ok(ventas);
		}

		[HttpGet("comprobantes/ventas")]
		public async Task<IActionResult> ListarComprobantesVentas()
		{
			var comprobantes = await _comprobanteVentasService.ListarComprobantesAsync();
			return Ok(comprobantes);
		}

		[HttpGet("comprobantes/ventas/{id}")]
		public async Task<IActionResult> ObtenerComprobanteVenta(string id)
		{
			try
			{
				var comprobante = await _comprobanteVentasService.ObtenerComprobantePorIdAsync(id);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return NotFound(new { mensaje = ex.Message });
			}
		}

		[HttpPost("comprobantes/ventas")]
		public async Task<IActionResult> EmitirComprobanteVenta([FromBody] ComprobanteVentaEmitirRequestDto request)
		{
			try
			{
				var comprobante = await _comprobanteVentasService.EmitirAsync(request);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}

		[HttpPost("comprobantes/ventas/{id}/sincronizar-sunat")]
		public async Task<IActionResult> SincronizarEstadoSunat(string id)
		{
			try
			{
				var comprobante = await _documentoFacturacionService.SincronizarEstadoSunatAsync(id);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}

		[HttpGet("comprobantes/ventas/{id}/pdf")]
		public async Task<IActionResult> DescargarPdfComprobanteVenta(string id, [FromQuery] string format = "A4")
		{
			try
			{
				var pdf = await _comprobanteVentasService.DescargarPdfAsync(id, format);
				return File(pdf.Content, pdf.ContentType, pdf.FileName);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}

		[HttpPost("comprobantes/ventas/{id}/anular")]
		public async Task<IActionResult> AnularComprobanteVenta(string id, [FromBody] ComprobanteVentaAnularRequestDto request)
		{
			try
			{
				var comprobante = await _documentoFacturacionService.AnularAsync(id, request.Reason);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}
	}
}
