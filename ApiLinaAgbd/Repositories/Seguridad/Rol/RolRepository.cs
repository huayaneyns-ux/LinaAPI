using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Rol
{
	public class RolRepository : IRolRepository
	{
		private readonly Conexion _conexion;

		public RolRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<RolSelectDto> Listar()
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

			return lista;
		}

		public RolSelectDto? Obtener(int id)
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

			return rol;
		}

		public int Insertar(RolInsertDto modelo)
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

				return idRol;
			}
		}

		public void Actualizar(RolUpdateDto modelo)
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
			}
		}

		public void Eliminar(int id)
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
			}
		}
	}
}
