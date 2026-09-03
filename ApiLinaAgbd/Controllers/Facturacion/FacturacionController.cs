
using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Models.Facturacion.Documentos;

using ApiLinaAgbd.Models.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Models.Facturacion.NotaCredito;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;

using ApiLinaAgbd.Services.Facturacion;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion
{
	[ApiController]
	[Route("api/facturacion")]
	public class FacturacionController : ControllerBase
	{
		private readonly ComprobanteVentasService _comprobanteVentasService;
		private readonly DocumentoFacturacionService _documentoFacturacionService;

		private readonly LiquidacionCompraService _liquidacionCompraService;
		private readonly NotaCreditoService _notaCreditoService;
		private readonly NotaDebitoService _notaDebitoService;

		public FacturacionController(
			ComprobanteVentasService comprobanteVentasService,
			DocumentoFacturacionService documentoFacturacionService,
			LiquidacionCompraService liquidacionCompraService,
			NotaCreditoService notaCreditoService,
			NotaDebitoService notaDebitoService)
		{
			_comprobanteVentasService = comprobanteVentasService;
			_documentoFacturacionService = documentoFacturacionService;
			_liquidacionCompraService = liquidacionCompraService;
			_notaCreditoService = notaCreditoService;
			_notaDebitoService = notaDebitoService;
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

		[HttpGet("comprobantes")]
		public async Task<IActionResult> ListarTodosLosComprobantes()
		{
			var comprobantes = await _documentoFacturacionService.ListarTodosAsync();
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

		[HttpGet("comprobantes/{id}")]
		public async Task<IActionResult> ObtenerDocumento(string id)
		{
			try
			{
				var comprobante = await _documentoFacturacionService.ObtenerDetalleAsync(id);
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

		[HttpPost("comprobantes/{id}/sincronizar-sunat")]
		public async Task<IActionResult> SincronizarDocumentoSunat(string id)
		{
			try
			{
				DocumentoFacturacionDto comprobante = await _documentoFacturacionService.SincronizarEstadoSunatAsync(id);
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

		[HttpGet("comprobantes/{id}/pdf")]
		public async Task<IActionResult> DescargarPdfDocumento(string id, [FromQuery] string format = "A4")
		{
			try
			{
				var pdf = await _documentoFacturacionService.DescargarPdfAsync(id, format);
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

		[HttpPost("comprobantes/{id}/anular")]
		public async Task<IActionResult> AnularDocumento(string id, [FromBody] ComprobanteVentaAnularRequestDto request)
		{
			try
			{
				DocumentoFacturacionDto comprobante = await _documentoFacturacionService.AnularAsync(id, request.Reason);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
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

		[HttpPost("comprobantes/notas/debito")]
		public async Task<IActionResult> EmitirNotaDebito([FromBody] NotaDebitoEmitirRequestDto request)
		{
			try
			{
				var resultado = await _notaDebitoService.EmitirAsync(request);
				return Ok(resultado);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}
	}
}
