using ApiLinaAgbd.Models.Facturacion.Boleta;
using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
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
		private readonly ComprobanteVentasService _comprobanteVentasService;
		private readonly FacturaService _facturaService;
		private readonly NotaDebitoService _notaDebitoService;

		public FacturacionController(
			BoletaService boletaService,
			ComprobanteVentasService comprobanteVentasService,
			FacturaService facturaService,
			NotaDebitoService notaDebitoService)
		{
			_boletaService = boletaService;
			_comprobanteVentasService = comprobanteVentasService;
			_facturaService = facturaService;
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
				var comprobante = await _comprobanteVentasService.SincronizarEstadoSunatAsync(id);
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
				var comprobante = await _comprobanteVentasService.AnularAsync(id, request.Reason);
				return Ok(comprobante);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
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
