using ApiLinaAgbd.Services.Ventas.VentaRealizada;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class VentaRealizadaController : ControllerBase
	{
		private readonly IVentaRealizadaService _ventaRealizadaService;

		public VentaRealizadaController(IVentaRealizadaService ventaRealizadaService)
		{
			_ventaRealizadaService = ventaRealizadaService;
		}

		//========================================
		// LISTAR VENTAS REALIZADAS
		//========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = _ventaRealizadaService.Listar();
			return Ok(lista);
		}

		//========================================
		// OBTENER VENTA POR ID
		//========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			var venta = _ventaRealizadaService.Obtener(id);

			if (venta == null)
				return NotFound("Venta no encontrada");

			return Ok(venta);
		}

		//========================================
		// DETALLE DE VENTA
		//========================================
		[HttpGet("{id}/Detalle")]
		public IActionResult Detalle(int id)
		{
			var lista = _ventaRealizadaService.Detalle(id);
			return Ok(lista);
		}

		//========================================
		// PAGOS DE UNA VENTA
		//========================================
		[HttpGet("{id}/Pago")]
		public IActionResult Pago(int id)
		{
			var lista = _ventaRealizadaService.Pago(id);
			return Ok(lista);
		}
	}
}
