using ApiLinaAgbd.Models.Ventas;
using ApiLinaAgbd.Services.Ventas.Lugares;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class LugaresController : ControllerBase
	{
		private readonly ILugaresService _lugaresService;

		public LugaresController(ILugaresService lugaresService)
		{
			_lugaresService = lugaresService;
		}

		//=========================================
		// LISTAR DEPARTAMENTOS
		//=========================================
		[HttpGet("Departamentos")]
		public IActionResult ListarDepartamentos()
		{
			var lista = _lugaresService.ListarDepartamentos();
			return Ok(lista);
		}

		//=========================================
		// LISTAR PROVINCIAS
		//=========================================
		[HttpGet("Provincias/{idDepartamento}")]
		public IActionResult ListarProvincias(int idDepartamento)
		{
			var lista = _lugaresService.ListarProvincias(idDepartamento);
			return Ok(lista);
		}

		//=========================================
		// LISTAR DISTRITOS
		//=========================================
		[HttpGet("Distritos/{idProvincia}")]
		public IActionResult ListarDistritos(int idProvincia)
		{
			var lista = _lugaresService.ListarDistritos(idProvincia);
			return Ok(lista);
		}

		//=========================================
		// LISTAR DIRECCIONES DEL USUARIO
		//=========================================
		[HttpGet("Direccion/{idUsuario}")]
		public IActionResult ListarDireccionesUsuario(int idUsuario)
		{
			var lista = _lugaresService.ListarDireccionesUsuario(idUsuario);
			return Ok(lista);
		}

		//=========================================
		// INSERTAR DIRECCION
		//=========================================
		[HttpPost("Direccion")]
		public IActionResult InsertarDireccion(DireccionInsertDto modelo)
		{
			try
			{
				_lugaresService.InsertarDireccion(modelo);

				return Ok(new
				{
					success = true,
					mensaje = "Dirección registrada correctamente."
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}
		}

		//=========================================
		// CAMBIAR DIRECCION PRINCIPAL
		//=========================================
		[HttpPut("Direccion/Principal")]
		public IActionResult CambiarPrincipal(DireccionPrincipalDto modelo)
		{
			try
			{
				_lugaresService.CambiarPrincipal(modelo);

				return Ok(new
				{
					success = true,
					mensaje = "Dirección principal actualizada correctamente."
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}
		}

		//=========================================
		// ELIMINAR DIRECCION
		//=========================================
		[HttpDelete("Direccion/{idUsuario}/{idDireccion}")]
		public IActionResult EliminarDireccion(
			int idUsuario,
			int idDireccion)
		{
			try
			{
				_lugaresService.EliminarDireccion(idUsuario, idDireccion);

				return Ok(new
				{
					success = true,
					mensaje = "Dirección eliminada correctamente."
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}
		}
	}
}
