using ApiLinaAgbd.Models.Ventas.Caja;
using ApiLinaAgbd.Services.Ventas.Caja;
using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Services.Facturacion.ComprobantesVenta;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class CajaController : ControllerBase
	{
		private readonly ICajaService _cajaService;
		private readonly IComprobanteVentasService _comprobanteVentasService;

		public CajaController(ICajaService cajaService, IComprobanteVentasService comprobanteVentasService)
		{
			_cajaService = cajaService;
			_comprobanteVentasService = comprobanteVentasService;
		}

		//================================================
		// REGISTRAR VENTA COMPLETA
		//================================================
		[HttpPost("RegistrarVenta")]
		public async Task<IActionResult> RegistrarVenta(
			[FromBody] CajaVentaInsertDto venta)
		{
			int idVenta = 0;

			try
			{
				idVenta = _cajaService.RegistrarVenta(venta);
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					mensaje = "Error al registrar venta",
					error = ex.Message
				});
			}

			var tipoComprobante = (venta.TipoComprobante ?? "BOLETA").Trim().ToUpperInvariant();
			var mensaje = "Venta registrada correctamente";
			if (tipoComprobante is "BOLETA" or "FACTURA")
			{
				try
				{
					var comprobante = await _comprobanteVentasService.EmitirAsync(new ComprobanteVentaEmitirRequestDto
					{
						Tipo = tipoComprobante,
						VentaOrigenId = idVenta,
						ReceptorSource = tipoComprobante == "FACTURA" && venta.ClienteFiscal is not null
							? "CUSTOMER"
							: "SALE_CUSTOMER",
						Cliente = venta.ClienteFiscal is null ? null : new ComprobanteVentaClienteDto
						{
							TipoDocumento = venta.ClienteFiscal.TipoDocumento,
							Documento = venta.ClienteFiscal.Documento,
							Nombre = venta.ClienteFiscal.Nombre,
							Direccion = venta.ClienteFiscal.Direccion,
							Correo = venta.ClienteFiscal.Correo
						},
						Pago = new ComprobanteVentaPagoDto
						{
							FormaPago = "CONTADO",
							Cuotas = new List<ComprobanteVentaCuotaDto>()
						}
					});
					mensaje = $"Venta registrada y {tipoComprobante.ToLowerInvariant()} enviada a SUNAT: {comprobante.EstadoSunat}.";
				}
				catch (InvalidOperationException ex)
				{
					return StatusCode(StatusCodes.Status502BadGateway, new
					{
						mensaje = $"La venta {idVenta} fue registrada, pero no se pudo enviar el comprobante a SUNAT.",
						detalle = ex.Message,
						idVenta
					});
				}
			}
			else if (tipoComprobante == "SIN_COMPROBANTE")
			{
				mensaje = "Venta registrada sin comprobante.";
			}

			return Ok(new CajaVentaResponseDto
			{
				IdVenta = idVenta,
				Mensaje = mensaje
			});
		}

		//================================================
		// BUSCAR CLIENTE POR DNI
		//================================================
		[HttpGet("Cliente/{dni}")]
		public IActionResult BuscarCliente(string dni)
		{
			var cliente = _cajaService.BuscarCliente(dni);

			if (cliente == null)
				return NotFound("Cliente no encontrado");

			return Ok(cliente);
		}

		[HttpGet("Cliente/{tipoDocumento}/{numero}")]
		public async Task<IActionResult> BuscarClientePorDocumento(string tipoDocumento, string numero)
		{
			var cliente = await _cajaService.BuscarClientePorDocumentoAsync(tipoDocumento, numero);
			return cliente is null ? NotFound(new { mensaje = "Documento no encontrado en BD ni ApiPeru." }) : Ok(cliente);
		}

		//================================================
		// CREAR CLIENTE
		//================================================
		[HttpPost("Cliente")]
		public async Task<IActionResult> CrearCliente(
			[FromBody] CajaClienteInsertDto cliente)
		{
			var resultado = await _cajaService.CrearClienteAsync(cliente);
			if (resultado is null)
				return BadRequest(new { mensaje = "No se pudo validar el documento en API Perú." });

			return Ok(new
			{
				idCliente = resultado.Id,
				cliente = resultado,
				mensaje = "Cliente registrado correctamente"
			});
		}

		[HttpPost("{id}/Pago")]
		public IActionResult RegistrarPago(
			int id,
			[FromBody] CajaPagoInsertDto pago)
		{
			_cajaService.RegistrarPago(id, pago);

			return Ok("Pago registrado correctamente.");
		}
	}
}
