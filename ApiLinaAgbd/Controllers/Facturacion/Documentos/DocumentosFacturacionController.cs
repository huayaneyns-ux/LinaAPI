using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Models.Facturacion.Documentos;
using ApiLinaAgbd.Services.Facturacion.Documentos;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion.Documentos
{
	[ApiController]
	[Route("api/facturacion")]
	[Tags("Facturacion - Documentos")]
	public class DocumentosFacturacionController : ControllerBase
	{
		private readonly IDocumentoFacturacionService _documentoFacturacionService;

		public DocumentosFacturacionController(IDocumentoFacturacionService documentoFacturacionService)
		{
			_documentoFacturacionService = documentoFacturacionService;
		}

		[HttpGet("comprobantes")]
		public async Task<IActionResult> ListarTodosLosComprobantes()
		{
			var comprobantes = await _documentoFacturacionService.ListarTodosAsync();
			return Ok(comprobantes);
		}

		[HttpGet("comprobantes/transmisiones-sunat")]
		public async Task<IActionResult> ListarTransmisionesSunat()
		{
			var transmisiones = await _documentoFacturacionService.ListarTransmisionesSunatAsync();
			return Ok(transmisiones);
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

		[HttpPost("comprobantes/{id}/reenviar-sunat")]
		public async Task<IActionResult> ReenviarDocumentoSunat(string id)
		{
			try
			{
				var comprobante = await _documentoFacturacionService.ReenviarAsync(id);
				return Ok(comprobante);
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
	}
}
