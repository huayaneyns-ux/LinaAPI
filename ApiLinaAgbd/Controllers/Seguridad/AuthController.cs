using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly Conexion _conexion;

		public AuthController(Conexion conexion)
		{
			_conexion = conexion;
		}

		[HttpPost("login")]
		public IActionResult Login([FromBody] UsuarioLoginDto modelo)
		{
			if (string.IsNullOrWhiteSpace(modelo.usuario) || string.IsNullOrWhiteSpace(modelo.contrasena))
			{
				return BadRequest(new { mensaje = "Usuario y contraseña son obligatorios." });
			}

			UsuarioLoginResponseDto? usuario = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				const string sql = @"
					SELECT TOP 1
						u.id,
						COALESCE(u.nombre_apellido, '') AS nombre_apellido,
						COALESCE(u.dni, '') AS dni,
						COALESCE(u.correo, '') AS correo,
						COALESCE(u.telefono, '') AS telefono,
						COALESCE(r.nombre, 'CLIENTE') AS rol,
						u.estado
					FROM Usuario u
					LEFT JOIN Rol r ON r.id = u.id_rol
					WHERE (u.correo = @usuario OR u.dni = @usuario)
					  AND u.contrasena = @contrasena;";

				SqlCommand cmd = new SqlCommand(sql, con);
				cmd.CommandType = CommandType.Text;
				cmd.Parameters.AddWithValue("@usuario", modelo.usuario.Trim());
				cmd.Parameters.AddWithValue("@contrasena", modelo.contrasena);

				SqlDataReader dr = cmd.ExecuteReader();
				if (dr.Read())
				{
					var nombreCompleto = dr["nombre_apellido"].ToString() ?? string.Empty;
					var partes = SepararNombre(nombreCompleto);
					var rol = NormalizarRol(dr["rol"].ToString());
					var estado = Convert.ToBoolean(dr["estado"]) ? "ACTIVO" : "INACTIVO";
					var correo = dr["correo"].ToString() ?? string.Empty;
					var dni = dr["dni"].ToString() ?? string.Empty;

					usuario = new UsuarioLoginResponseDto
					{
						id = dr["id"].ToString() ?? string.Empty,
						username = !string.IsNullOrWhiteSpace(correo) ? correo : dni,
						nombres = partes.nombres,
						apellidos = partes.apellidos,
						rol = rol,
						email = correo,
						estado = estado,
						sucursal = string.Empty,
						telefono = dr["telefono"] == DBNull.Value ? null : dr["telefono"].ToString(),
						createdAt = DateTime.UtcNow.ToString("o"),
						updatedAt = DateTime.UtcNow.ToString("o")
					};
				}
			}

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

		private static (string nombres, string apellidos) SepararNombre(string nombreCompleto)
		{
			var limpio = (nombreCompleto ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(limpio))
			{
				return (string.Empty, string.Empty);
			}

			var partes = limpio.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (partes.Length == 1)
			{
				return (partes[0], string.Empty);
			}

			if (partes.Length == 2)
			{
				return (partes[0], partes[1]);
			}

			return (string.Join(' ', partes.Take(partes.Length - 2)), string.Join(' ', partes.Skip(partes.Length - 2)));
		}

		private static string NormalizarRol(string? rol)
		{
			return (rol ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"ADMINISTRADOR" => "ADMINISTRADOR",
				"TRABAJADOR" => "TRABAJADOR",
				"SUPERVISOR" => "SUPERVISOR",
				"CAJERO" => "CAJERO",
				_ => "CLIENTE"
			};
		}
	}
}
