using ApiLinaAgbd.Models.Compras.Compra;
using ApiLinaAgbd.Services.Compras.Compra;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Compras
{
	[ApiController]
	[Route("api/[controller]")]
	public class CompraController : ControllerBase
	{
		private readonly ICompraService _compraService;

		public CompraController(ICompraService compraService)
		{
			_compraService = compraService;
		}

		[HttpPost("RegistrarCompleta")]
		public IActionResult RegistrarCompleta(CompraCompletaInsertDto modelo)
		{
			var result = _compraService.RegistrarCompleta(modelo);

			if (!result.Success)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = result.Mensaje
				});
			}

			return Ok(new
			{
				success = true,
				mensaje = result.Mensaje,
				idCompra = result.IdCompra
			});
		}

		[HttpGet("{id}/Detalle")]
		public IActionResult Detalle(int id)
		{
			var lista = _compraService.ObtenerDetalle(id);
			return Ok(lista);
		}

		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = _compraService.Listar();
			return Ok(lista);
		}
	}
}
