using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.Marca;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class MarcaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public MarcaController(Conexion conexion)
		{
			_conexion = conexion;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarMarcas()
		{
			List<MarcaSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_MARCA_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new MarcaSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),
						UrlImagen = dr["url_imagen"] == DBNull.Value ? null : dr["url_imagen"].ToString()
					});
				}
			}

			return Ok(lista);
		}

		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarMarca([FromBody] MarcaInsertDto marca)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_INS_MARCA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Nombre", marca.Nombre);
				cmd.Parameters.AddWithValue("@UrlImagen", (object?)marca.UrlImagen ?? DBNull.Value);

				cmd.ExecuteNonQuery();
			}

			return Ok("Marca registrada correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarMarca([FromBody] MarcaUpdateDto marca)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_UPD_MARCA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", marca.Id);
				cmd.Parameters.AddWithValue("@Nombre", marca.Nombre);
				cmd.Parameters.AddWithValue("@Estado", marca.Estado);
				cmd.Parameters.AddWithValue("@UrlImagen", (object?)marca.UrlImagen ?? DBNull.Value);

				cmd.ExecuteNonQuery();
			}

			return Ok("Marca actualizada correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarMarca(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_MARCA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				cmd.ExecuteNonQuery();
			}

			return Ok("Marca eliminada correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerMarcaPorId(int id)
		{
			MarcaObtenerIDDto marca = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_MARCA_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					marca = new MarcaObtenerIDDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),
						UrlImagen = dr["url_imagen"] == DBNull.Value ? null : dr["url_imagen"].ToString()
					};
				}
			}

			if (marca == null)
				return NotFound("Marca no encontrada.");

			return Ok(marca);
		}
	}
}