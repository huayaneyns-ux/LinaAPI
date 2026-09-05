using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Services.Seguridad.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;

		public AuthController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("login")]
		public IActionResult Login([FromBody] UsuarioLoginDto modelo)
		{
			if (string.IsNullOrWhiteSpace(modelo.usuario) || string.IsNullOrWhiteSpace(modelo.contrasena))
			{
				return BadRequest(new { mensaje = "Usuario y contraseña son obligatorios." });
			}

			var usuario = _authService.Login(modelo);

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
	}
}
