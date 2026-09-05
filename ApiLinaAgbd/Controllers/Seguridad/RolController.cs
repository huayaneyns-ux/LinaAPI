using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Services.Seguridad.Rol;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad
{
	[ApiController]
	[Route("api/[controller]")]
	public class RolController : ControllerBase
	{
		private readonly IRolService _rolService;

		public RolController(IRolService rolService)
		{
			_rolService = rolService;
		}

		//=========================================
		// LISTAR ROLES
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = _rolService.Listar();
			return Ok(lista);
		}

		//=========================================
		// OBTENER ROL
		//=========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			var rol = _rolService.Obtener(id);

			if (rol == null)
				return NotFound();

			return Ok(rol);
		}

		//=========================================
		// INSERTAR ROL
		//=========================================
		[HttpPost("Insertar")]
		public IActionResult Insertar(
			RolInsertDto modelo
		)
		{
			var idRol = _rolService.Insertar(modelo);

			return Ok(new
			{
				success = true,
				mensaje = "Rol registrado correctamente.",
				idRol
			});
		}

		//=========================================
		// ACTUALIZAR ROL
		//=========================================
		[HttpPut("Actualizar")]
		public IActionResult Actualizar(
			RolUpdateDto modelo
		)
		{
			_rolService.Actualizar(modelo);

			return Ok(new
			{
				success = true,
				mensaje = "Rol actualizado correctamente."
			});
		}

		//=========================================
		// ELIMINAR ROL (CAMBIA ESTADO)
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult Eliminar(int id)
		{
			_rolService.Eliminar(id);

			return Ok(new
			{
				success = true,
				mensaje = "Rol eliminado correctamente."
			});
		}
	}
}
