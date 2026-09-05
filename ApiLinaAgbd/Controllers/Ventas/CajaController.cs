using ApiLinaAgbd.Models.Ventas.Caja;
using ApiLinaAgbd.Services.Ventas.Caja;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class CajaController : ControllerBase
	{
		private readonly ICajaService _cajaService;

		public CajaController(ICajaService cajaService)
		{
			_cajaService = cajaService;
		}

		//================================================
		// REGISTRAR VENTA COMPLETA
		//================================================
		[HttpPost("RegistrarVenta")]
		public IActionResult RegistrarVenta(
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

			return Ok(new CajaVentaResponseDto
			{
				IdVenta = idVenta,
				Mensaje = "Venta registrada correctamente"
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

		//================================================
		// CREAR CLIENTE
		//================================================
		[HttpPost("Cliente")]
		public IActionResult CrearCliente(
			[FromBody] CajaClienteInsertDto cliente)
		{
			int idUsuario = _cajaService.CrearCliente(cliente);

			return Ok(new
			{
				idCliente = idUsuario,
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
