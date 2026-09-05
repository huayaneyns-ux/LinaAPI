using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.Marca;

namespace ApiLinaAgbd.Repositories.Inventario.Marca
{
	public class MarcaRepository : IMarcaRepository
	{
		private readonly Conexion _conexion;

		public MarcaRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<MarcaSelectDto> Listar()
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

			return lista;
		}

		public void Insertar(MarcaInsertDto marca)
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
		}

		public void Actualizar(MarcaUpdateDto marca)
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
		}

		public void Eliminar(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_MARCA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				cmd.ExecuteNonQuery();
			}
		}

		public MarcaObtenerIDDto? ObtenerPorId(int id)
		{
			MarcaObtenerIDDto? marca = null;

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

			return marca;
		}
	}
}
