using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class LugaresController : ControllerBase
	{
		private readonly Conexion _conexion;

		public LugaresController(Conexion conexion)
		{
			_conexion = conexion;
		}

		//=========================================
		// LISTAR DEPARTAMENTOS
		//=========================================
		[HttpGet("Departamentos")]
		public IActionResult ListarDepartamentos()
		{
			var lista = new List<DepartamentoDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DEP_SEL_DEPARTAMENTO_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new DepartamentoDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty
					});
				}

				dr.Close();
			}

			return Ok(lista);
		}


		//=========================================
		// LISTAR PROVINCIAS
		//=========================================
		[HttpGet("Provincias/{idDepartamento}")]
		public IActionResult ListarProvincias(int idDepartamento)
		{
			var lista = new List<ProvinciaDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PROV_SEL_PROVINCIA_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdDepartamento",
					idDepartamento);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new ProvinciaDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty,
						idDepartamento = Convert.ToInt32(dr["idDepartamento"])
					});
				}

				dr.Close();
			}

			return Ok(lista);
		}


		//=========================================
		// LISTAR DISTRITOS
		//=========================================
		[HttpGet("Distritos/{idProvincia}")]
		public IActionResult ListarDistritos(int idProvincia)
		{
			var lista = new List<DistritoDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIS_SEL_DISTRITO_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdProvincia",
					idProvincia);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new DistritoDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty,
						idProvincia = Convert.ToInt32(dr["idProvincia"])
					});
				}

				dr.Close();
			}

			return Ok(lista);
		}
		//=========================================
		// LISTAR DIRECCIONES DEL USUARIO
		//=========================================
		[HttpGet("Direccion/{idUsuario}")]
		public IActionResult ListarDireccionesUsuario(int idUsuario)
		{
			var lista = new List<DireccionDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIR_SEL_DIRECCION_USUARIO",
					con);

				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					idUsuario);


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new DireccionDto
					{
						id = Convert.ToInt32(dr["id"]),

						nombreDireccion = dr["nombre_direccion"].ToString()
							?? string.Empty,

						referencia = dr["referencia"].ToString()
							?? string.Empty,


						idDistrito = Convert.ToInt32(dr["id_distrito"]),

						distrito = dr["distrito"].ToString()
							?? string.Empty,


						idProvincia = Convert.ToInt32(dr["id_provincia"]),

						provincia = dr["provincia"].ToString()
							?? string.Empty,


						idDepartamento = Convert.ToInt32(dr["id_departamento"]),

						departamento = dr["departamento"].ToString()
							?? string.Empty,


						esPrincipal = Convert.ToBoolean(dr["es_principal"])

					});
				}

				dr.Close();
			}

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
				using (SqlConnection con = _conexion.ObtenerConexion())
				{
					con.Open();


					SqlCommand cmd = new SqlCommand(
						"USP_DIR_INS_DIRECCION_USUARIO",
						con);


					cmd.CommandType = CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@IdUsuario",
						modelo.idUsuario);


					cmd.Parameters.AddWithValue(
						"@NombreDireccion",
						modelo.nombreDireccion);


					cmd.Parameters.AddWithValue(
						"@Referencia",
						modelo.referencia);


					cmd.Parameters.AddWithValue(
						"@IdDistrito",
						modelo.idDistrito);


					cmd.Parameters.AddWithValue(
						"@EsPrincipal",
						modelo.esPrincipal);



					cmd.ExecuteNonQuery();

				}


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
				using (SqlConnection con = _conexion.ObtenerConexion())
				{
					con.Open();


					SqlCommand cmd = new SqlCommand(
						"USP_DIR_UPD_CAMBIAR_PRINCIPAL",
						con);


					cmd.CommandType = CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@IdUsuario",
						modelo.idUsuario);


					cmd.Parameters.AddWithValue(
						"@IdDireccion",
						modelo.idDireccion);



					cmd.ExecuteNonQuery();

				}


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
				using (SqlConnection con = _conexion.ObtenerConexion())
				{
					con.Open();


					SqlCommand cmd = new SqlCommand(
						"USP_DIR_DEL_DIRECCION_USUARIO",
						con);


					cmd.CommandType = CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@IdUsuario",
						idUsuario);


					cmd.Parameters.AddWithValue(
						"@IdDireccion",
						idDireccion);



					cmd.ExecuteNonQuery();

				}


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