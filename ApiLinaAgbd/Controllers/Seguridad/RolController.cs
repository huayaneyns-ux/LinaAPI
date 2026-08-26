using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;
using Microsoft.AspNetCore.Mvc;


namespace ApiLinaAgbd.Controllers.Seguridad
{

	[ApiController]
	[Route("api/[controller]")]
	public class RolController : ControllerBase
	{

		private readonly Conexion _conexion;


		public RolController(Conexion conexion)
		{
			_conexion = conexion;
		}



		//=========================================
		// LISTAR ROLES
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{

			List<RolSelectDto> lista = new();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_ROL_SEL_ROLES_LISTAR",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;



				SqlDataReader dr = cmd.ExecuteReader();



				while (dr.Read())
				{

					lista.Add(new RolSelectDto
					{

						id = Convert.ToInt32(
							dr["id"]
						),


						nombre = dr["nombre"]
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
		// OBTENER ROL
		//=========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{

			RolSelectDto? rol = null;


			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_ROL_SEL_ROL_OBTENER",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@IdRol",
					id
				);



				SqlDataReader dr = cmd.ExecuteReader();



				if (dr.Read())
				{

					rol = new RolSelectDto
					{

						id = Convert.ToInt32(
							dr["id"]
						),


						nombre = dr["nombre"]
							.ToString() ?? "",


						estado = Convert.ToBoolean(
							dr["estado"]
						)

					};

				}

			}


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

			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_ROL_INS_ROL",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@Nombre",
					modelo.nombre
				);



				SqlDataReader dr = cmd.ExecuteReader();


				int idRol = 0;



				if (dr.Read())
				{

					idRol = Convert.ToInt32(
						dr["IdRol"]
					);

				}



				return Ok(new
				{
					success = true,
					mensaje = "Rol registrado correctamente.",
					idRol
				});

			}

		}





		//=========================================
		// ACTUALIZAR ROL
		//=========================================
		[HttpPut("Actualizar")]
		public IActionResult Actualizar(
			RolUpdateDto modelo
		)
		{

			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_ROL_UPD_ROL",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@IdRol",
					modelo.id
				);


				cmd.Parameters.AddWithValue(
					"@Nombre",
					modelo.nombre
				);



				cmd.ExecuteNonQuery();



				return Ok(new
				{
					success = true,
					mensaje = "Rol actualizado correctamente."
				});

			}

		}





		//=========================================
		// ELIMINAR ROL (CAMBIA ESTADO)
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult Eliminar(int id)
		{

			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_ROL_DEL_ROL",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;



				cmd.Parameters.AddWithValue(
					"@IdRol",
					id
				);



				cmd.ExecuteNonQuery();



				return Ok(new
				{
					success = true,
					mensaje = "Rol eliminado correctamente."
				});

			}

		}


	}

}