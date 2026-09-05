using Microsoft.AspNetCore.Mvc;
using ApiLinaAgbd.Models.Inventario.Productos;
using ApiLinaAgbd.Services.Inventario.Producto;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProductoController : ControllerBase
	{
		private readonly IProductoService _productoService;

		public ProductoController(IProductoService productoService)
		{
			_productoService = productoService;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ObtenerProductos()
		{
			var lista = _productoService.Listar();
			return Ok(lista);
		}

		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarProducto([FromBody] ProductoInsertDto producto)
		{
			_productoService.Insertar(producto);
			return Ok("Producto registrado correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarProducto([FromBody] ProductoUpdateDto producto)
		{
			_productoService.Actualizar(producto);
			return Ok("Producto actualizado correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarProducto(int id)
		{
			_productoService.Eliminar(id);
			return Ok("Producto eliminado correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================

		[HttpGet("{id}")]
		public IActionResult ObtenerProductoPorId(int id)
		{
			var producto = _productoService.ObtenerPorId(id);

			if (producto == null)
				return NotFound("Producto no encontrado.");

			return Ok(producto);
		}
	}
}
