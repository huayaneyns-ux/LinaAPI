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