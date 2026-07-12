using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.Categorias;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class CategoriaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public CategoriaController(Conexion conexion)
		{
			_conexion = conexion;
		}

		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarCategorias()
		{
			List<CategoriaSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_CATEGORIA_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new CategoriaSelectDto
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
		public IActionResult InsertarCategoria([FromBody] CategoriaInsertDto categoria)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_INS_CATEGORIA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Nombre", categoria.Nombre);
				cmd.Parameters.AddWithValue("@UrlImagen", (object?)categoria.UrlImagen ?? DBNull.Value);

				cmd.ExecuteNonQuery();
			}

			return Ok("Categoría registrada correctamente.");
		}

		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarCategoria([FromBody] CategoriaUpdateDto categoria)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_UPD_CATEGORIA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", categoria.Id);
				cmd.Parameters.AddWithValue("@Nombre", categoria.Nombre);
				cmd.Parameters.AddWithValue("@Estado", categoria.Estado);
				cmd.Parameters.AddWithValue("@UrlImagen", (object?)categoria.UrlImagen ?? DBNull.Value);

				cmd.ExecuteNonQuery();
			}

			return Ok("Categoría actualizada correctamente.");
		}

		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarCategoria(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_CATEGORIA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				cmd.ExecuteNonQuery();
			}

			return Ok("Categoría eliminada correctamente.");
		}

		//=========================================
		// OBTENER
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerCategoriaPorId(int id)
		{
			CategoriaObtenerIDDto categoria = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_CATEGORIA_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					categoria = new CategoriaObtenerIDDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),
						UrlImagen = dr["url_imagen"] == DBNull.Value ? null : dr["url_imagen"].ToString()
					};
				}
			}

			if (categoria == null)
				return NotFound("Categoría no encontrada.");

			return Ok(categoria);
		}
	}
}