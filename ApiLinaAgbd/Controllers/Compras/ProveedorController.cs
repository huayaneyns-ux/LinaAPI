using System.Text.Json;
using ApiLinaAgbd.Models.Compras.Proveedor;
using ApiLinaAgbd.Services.Compras.Proveedor;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Compras
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProveedorController : ControllerBase
	{
		private readonly IProveedorService _proveedorService;

		public ProveedorController(IProveedorService proveedorService)
		{
			_proveedorService = proveedorService;
		}

		[HttpGet]
		public IActionResult Listar()
		{
			var lista = _proveedorService.Listar();
			return Ok(lista);
		}

		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			var proveedor = _proveedorService.Obtener(id);

			if (proveedor == null)
				return NotFound("Proveedor no encontrado");

			return Ok(proveedor);
		}

		[HttpPost]
		public IActionResult Insertar([FromBody] dynamic data)
		{
			JsonElement json = (JsonElement)data;
			_proveedorService.Insertar(json);
			return Ok("Proveedor registrado correctamente");
		}

		[HttpPut]
		public IActionResult Actualizar([FromBody] ProveedorUpdate proveedor)
		{
			_proveedorService.Actualizar(proveedor);
			return Ok("Proveedor actualizado correctamente");
		}

		[HttpDelete("{id}")]
		public IActionResult Eliminar(int id)
		{
			_proveedorService.Eliminar(id);
			return Ok("Proveedor desactivado correctamente");
		}
	}
}
