using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.UnidadMedida;

namespace ApiLinaAgbd.Repositories.Inventario.UnidadMedida
{
	public class UnidadMedidaRepository : IUnidadMedidaRepository
	{
		private readonly Conexion _conexion;

		public UnidadMedidaRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<UnidadMedidaSelectDto> Listar()
		{
			List<UnidadMedidaSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_SEL_UNIDAD_MEDIDA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new UnidadMedidaSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Abreviatura = dr["abreviatura"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"])
					});
				}
			}

			return lista;
		}

		public void Insertar(UnidadMedidaInsertDto unidad)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_INS_UNIDAD_MEDIDA",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@nombre",
					unidad.Nombre);

				cmd.Parameters.AddWithValue(
					"@abreviatura",
					unidad.Abreviatura);

				cmd.ExecuteNonQuery();
			}
		}

		public void Actualizar(UnidadMedidaUpdateDto unidad)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_UPD_UNIDAD_MEDIDA",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@id",
					unidad.Id);

				cmd.Parameters.AddWithValue(
					"@nombre",
					unidad.Nombre);

				cmd.Parameters.AddWithValue(
					"@abreviatura",
					unidad.Abreviatura);

				cmd.ExecuteNonQuery();
			}
		}

		public void Eliminar(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DEL_UNIDAD_MEDIDA",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@id",
					id);

				cmd.ExecuteNonQuery();
			}
		}

		public UnidadMedidaObtenerIdDto? ObtenerPorId(int id)
		{
			UnidadMedidaObtenerIdDto? unidad = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_SEL_UNIDAD_MEDIDA_ID",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@id",
					id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					unidad = new UnidadMedidaObtenerIdDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Abreviatura = dr["abreviatura"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"])
					};
				}
			}

			return unidad;
		}
	}
}
