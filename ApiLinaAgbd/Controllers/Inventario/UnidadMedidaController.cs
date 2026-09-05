using ApiLinaAgbd.Models.Inventario.UnidadMedida;
using ApiLinaAgbd.Services.Inventario.UnidadMedida;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class UnidadMedidaController : ControllerBase
	{
		private readonly IUnidadMedidaService _unidadMedidaService;

		public UnidadMedidaController(IUnidadMedidaService unidadMedidaService)
		{
			_unidadMedidaService = unidadMedidaService;
		}


		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarUnidadMedidas()
		{
			var lista = _unidadMedidaService.Listar();
			return Ok(lista);
		}



		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarUnidadMedida(
			[FromBody] UnidadMedidaInsertDto unidad)
		{
			_unidadMedidaService.Insertar(unidad);
			return Ok("Unidad de medida registrada correctamente.");
		}





		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarUnidadMedida(
			[FromBody] UnidadMedidaUpdateDto unidad)
		{
			_unidadMedidaService.Actualizar(unidad);
			return Ok("Unidad de medida actualizada correctamente.");
		}





		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarUnidadMedida(int id)
		{
			_unidadMedidaService.Eliminar(id);
			return Ok("Unidad de medida eliminada correctamente.");
		}





		//=========================================
		// OBTENER POR ID
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerUnidadMedidaPorId(int id)
		{
			var unidad = _unidadMedidaService.ObtenerPorId(id);

			if (unidad == null)
				return NotFound("Unidad de medida no encontrada.");

			return Ok(unidad);
		}
	}
}
