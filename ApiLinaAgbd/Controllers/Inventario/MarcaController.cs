using ApiLinaAgbd.Models.Inventario.Marca;
using ApiLinaAgbd.Services.Inventario.Marca;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class MarcaController : ControllerBase
	{
		private readonly IMarcaService _marcaService;

		public MarcaController(IMarcaService marcaService)
		{
			_marcaService = marcaService;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarMarcas()
		{
			var lista = _marcaService.Listar();
			return Ok(lista);
		}

		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarMarca([FromBody] MarcaInsertDto marca)
		{
			_marcaService.Insertar(marca);
			return Ok("Marca registrada correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarMarca([FromBody] MarcaUpdateDto marca)
		{
			_marcaService.Actualizar(marca);
			return Ok("Marca actualizada correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarMarca(int id)
		{
			_marcaService.Eliminar(id);
			return Ok("Marca eliminada correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerMarcaPorId(int id)
		{
			var marca = _marcaService.ObtenerPorId(id);

			if (marca == null)
				return NotFound("Marca no encontrada.");

			return Ok(marca);
		}
	}
}
