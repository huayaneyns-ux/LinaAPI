using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad
{

	[ApiController]
	[Route("api/[controller]")]
	public class UsuarioController : ControllerBase
	{

		private readonly Conexion _conexion;


		public UsuarioController(Conexion conexion)
		{
			_conexion = conexion;
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

		//=========================================
		// LISTAR USUARIOS
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			List<UsuarioSelectDto> lista = new();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_USU_SEL_USUARIO_LISTAR",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new UsuarioSelectDto
					{

						id = Convert.ToInt32(dr["id_usuario"]),

						nombreApellido = dr["nombre_apellido"]
							.ToString() ?? "",

						dni = dr["dni"]
							.ToString() ?? "",

						sexo = dr["sexo"]
							.ToString() ?? "",

						telefono = dr["telefono"] == DBNull.Value
							? null
							: dr["telefono"].ToString(),

						correo = dr["correo"]
							.ToString() ?? "",

						idRol = Convert.ToInt32(dr["id_rol"]),

						rol = dr["rol"]
							.ToString() ?? "",

						estado = Convert.ToBoolean(
							dr["estado"]
						)

					});
				}

			}


			return Ok(lista);
		}





		//=========================================
		// OBTENER USUARIO
		//=========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{

			UsuarioSelectDto? usuario = null;


			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_USU_SEL_USUARIO_OBTENER",
					con
				);


				cmd.CommandType =
					CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					id
				);



				SqlDataReader dr =
					cmd.ExecuteReader();



				if (dr.Read())
				{

					usuario = new UsuarioSelectDto
					{

						id = Convert.ToInt32(dr["id"]),

						nombreApellido = dr["nombre_apellido"]
							.ToString() ?? "",

						dni = dr["dni"]
							.ToString() ?? "",

						sexo = dr["sexo"]
							.ToString() ?? "",

						telefono = dr["telefono"] == DBNull.Value
							? null
							: dr["telefono"].ToString(),

						correo = dr["correo"]
							.ToString() ?? "",


						idRol = Convert.ToInt32(
							dr["id_rol"]
						),


						rol = dr["rol"]
							.ToString() ?? "",


						estado = Convert.ToBoolean(
							dr["estado"]
						)

					};

				}

			}


			if (usuario == null)
				return NotFound();


			return Ok(usuario);

		}

		private static (string nombres, string apellidos) SepararNombre(string nombreCompleto)
		{
			var limpio = (nombreCompleto ?? string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(limpio))
			{
				return (string.Empty, string.Empty);
			}

			var partes = limpio
				.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (partes.Length == 1)
			{
				return (partes[0], string.Empty);
			}

			if (partes.Length == 2)
			{
				return (partes[0], partes[1]);
			}

			var nombres = string.Join(' ', partes.Take(partes.Length - 2));
			var apellidos = string.Join(' ', partes.Skip(partes.Length - 2));
			return (nombres, apellidos);
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





		//=========================================
		// INSERTAR / ACTUALIZAR USUARIO
		//=========================================
		[HttpPost("Guardar")]
		public IActionResult Guardar(
			UsuarioInsertUpdateDto modelo
		)
		{

			using (SqlConnection con =
				_conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_USU_INS_UPD_USUARIO",
					con
				);


				cmd.CommandType =
					CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					(object?)modelo.idUsuario ?? DBNull.Value
				);


				cmd.Parameters.AddWithValue(
					"@NombreApellido",
					modelo.nombreApellido
				);


				cmd.Parameters.AddWithValue(
					"@DNI",
					modelo.dni
				);


				cmd.Parameters.AddWithValue(
					"@Sexo",
					modelo.sexo
				);


				cmd.Parameters.AddWithValue(
					"@Telefono",
					(object?)modelo.telefono ?? DBNull.Value
				);


				cmd.Parameters.AddWithValue(
					"@Correo",
					modelo.correo
				);


				cmd.Parameters.AddWithValue(
					"@Contrasena",
					modelo.contrasena
				);


				cmd.Parameters.AddWithValue(
					"@IdRol",
					modelo.idRol
				);


				cmd.Parameters.AddWithValue(
					"@Estado",
					modelo.estado
				);



				SqlDataReader dr =
					cmd.ExecuteReader();


				int idUsuario = 0;


				if (dr.Read())
				{
					idUsuario =
						Convert.ToInt32(
							dr["IdUsuario"]
						);
				}



				return Ok(new
				{
					success = true,
					idUsuario
				});

			}

		}





		//=========================================
		// ELIMINAR USUARIO
		//=========================================
		[HttpDelete("Eliminar/{id}")]
		public IActionResult Eliminar(int id)
		{

			using (SqlConnection con =
				_conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_USU_DEL_USUARIO",
					con
				);


				cmd.CommandType =
					CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					id
				);


				cmd.ExecuteNonQuery();

			}


			return Ok(new
			{
				success = true,
				mensaje = "Usuario eliminado correctamente"
			});

		}

	}
}
