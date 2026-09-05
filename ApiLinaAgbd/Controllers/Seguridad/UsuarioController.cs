using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Services.Seguridad.Usuario;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsuarioController : ControllerBase
	{
		private readonly IUsuarioService _usuarioService;

		public UsuarioController(IUsuarioService usuarioService)
		{
			_usuarioService = usuarioService;
		}

		//=========================================
		// LOGIN
		//=========================================
		[HttpPost("Login")]
		public IActionResult Login([FromBody] UsuarioLoginDto modelo)
		{
			if (string.IsNullOrWhiteSpace(modelo.usuario) || string.IsNullOrWhiteSpace(modelo.contrasena))
			{
				return BadRequest(new { mensaje = "Usuario y contraseña son obligatorios." });
			}

			var usuario = _usuarioService.Login(modelo);

			if (usuario is null)
			{
				return Unauthorized(new { mensaje = "Credenciales inválidas." });
			}

			if (!string.Equals(usuario.estado, "ACTIVO", StringComparison.OrdinalIgnoreCase))
			{
				return Unauthorized(new { mensaje = "El usuario está inactivo." });
			}

			return Ok(usuario);
		}

		//=========================================
		// LISTAR USUARIOS
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = _usuarioService.Listar();
			return Ok(lista);
		}

		//=========================================
		// OBTENER USUARIO
		//=========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			var usuario = _usuarioService.Obtener(id);

			if (usuario == null)
				return NotFound();

			return Ok(usuario);
		}

		//=========================================
		// INSERTAR / ACTUALIZAR USUARIO
		//=========================================
		[HttpPost("Guardar")]
		public IActionResult Guardar(
			UsuarioInsertUpdateDto modelo
		)
		{
			var idUsuario = _usuarioService.Guardar(modelo);

			return Ok(new
			{
				success = true,
				idUsuario
			});
		}

		//=========================================
		// ELIMINAR USUARIO
		//=========================================
		[HttpDelete("Eliminar/{id}")]
		public IActionResult Eliminar(int id)
		{
			_usuarioService.Eliminar(id);

			return Ok(new
			{
				success = true,
				mensaje = "Usuario eliminado correctamente"
			});
		}
	}
}
