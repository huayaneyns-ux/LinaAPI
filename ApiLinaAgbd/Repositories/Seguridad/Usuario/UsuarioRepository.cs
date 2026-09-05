using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Usuario
{
	public class UsuarioRepository : IUsuarioRepository
	{
		private readonly Conexion _conexion;

		public UsuarioRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<UsuarioSelectDto> Listar()
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

			return lista;
		}

		public UsuarioSelectDto? Obtener(int id)
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

			return usuario;
		}

		public int Guardar(UsuarioInsertUpdateDto modelo)
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

				return idUsuario;
			}
		}

		public void Eliminar(int id)
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
		}
	}
}
