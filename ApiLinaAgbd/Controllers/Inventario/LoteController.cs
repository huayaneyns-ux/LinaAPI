using ApiLinaAgbd.Models.Inventario.Lote;
using ApiLinaAgbd.Models.Inventario.Lote_Stock;
using ApiLinaAgbd.Services.Inventario.Lote;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class LoteController : ControllerBase
	{
		private readonly ILoteService _loteService;

		public LoteController(ILoteService loteService)
		{
			_loteService = loteService;
		}

		//=========================================
		// INSERTAR LOTE
		//=========================================
		[HttpPost("Insertar")]
		public IActionResult Insertar(LoteInsertDto modelo)
		{
			var (idLote, codigoLote) = _loteService.Insertar(modelo);

			return Ok(new
			{
				success = true,
				mensaje = "Lote registrado correctamente.",
				idLote,
				codigoLote
			});
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarLotes(
			string? codigoLote = null,
			int? idProducto = null,
			int? idProveedor = null,
			DateTime? fechaIngresoDesde = null,
			DateTime? fechaIngresoHasta = null,
			DateTime? fechaVencimientoDesde = null,
			DateTime? fechaVencimientoHasta = null)
		{
			var filtro = new LoteFiltroDto
			{
				codigoLote = codigoLote,
				idProducto = idProducto,
				idProveedor = idProveedor,
				fechaIngresoDesde = fechaIngresoDesde,
				fechaIngresoHasta = fechaIngresoHasta,
				fechaVencimientoDesde = fechaVencimientoDesde,
				fechaVencimientoHasta = fechaVencimientoHasta
			};

			var lista = _loteService.Listar(filtro);
			return Ok(lista);
		}
		//=========================================
		// OBTENER LOTE
		//=========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			var lote = _loteService.Obtener(id);

			if (lote == null)
				return NotFound();

			return Ok(lote);
		}
		//=========================================
		// INSERTAR MOVIMIENTO
		//=========================================
		[HttpPost("InsertarMovimiento")]
		public IActionResult InsertarMovimiento(MovimientoInsertDto modelo)
		{
			var idMovimiento = _loteService.InsertarMovimiento(modelo);

			return Ok(new
			{
				success = true,
				mensaje = "Movimiento registrado correctamente.",
				idMovimiento
			});
		}

		//=========================================
		// LISTAR MOVIMIENTOS
		//=========================================
		[HttpGet("Lista-movimiento")]
		public IActionResult ListarMovimientos(
			int? idProducto = null,
			int? tipo = null,
			DateTime? fechaDesde = null,
			DateTime? fechaHasta = null)
		{
			var lista = _loteService.ListarMovimientos(idProducto, tipo, fechaDesde, fechaHasta);
			return Ok(lista);
		}
	}
}
