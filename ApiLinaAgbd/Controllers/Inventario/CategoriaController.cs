using Microsoft.AspNetCore.Mvc;
using ApiLinaAgbd.Models.Inventario.Categorias;
using ApiLinaAgbd.Services.Inventario.Categoria;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class CategoriaController : ControllerBase
	{
		private readonly ICategoriaService _categoriaService;

		public CategoriaController(ICategoriaService categoriaService)
		{
			_categoriaService = categoriaService;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarCategorias()
		{
			var lista = _categoriaService.Listar();
			return Ok(lista);
		}

		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarCategoria([FromBody] CategoriaInsertDto categoria)
		{
			_categoriaService.Insertar(categoria);
			return Ok("Categoría registrada correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarCategoria([FromBody] CategoriaUpdateDto categoria)
		{
			_categoriaService.Actualizar(categoria);
			return Ok("Categoría actualizada correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarCategoria(int id)
		{
			_categoriaService.Eliminar(id);
			return Ok("Categoría eliminada correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerCategoriaPorId(int id)
		{
			var categoria = _categoriaService.ObtenerPorId(id);

			if (categoria == null)
				return NotFound("Categoría no encontrada.");

			return Ok(categoria);
		}
	}
}
