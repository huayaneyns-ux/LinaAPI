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

				const string sql = @"
					SELECT
						u.id AS id_usuario,
						u.nombre_apellido,
						COALESCE(d.numero, '') AS dni,
						COALESCE(u.sexo, '') AS sexo,
						u.telefono,
						COALESCE(u.correo, '') AS correo,
						u.id_rol,
						COALESCE(r.nombre, '') AS rol,
						u.estado
					FROM dbo.usuario u
					LEFT JOIN dbo.documento d ON d.id = u.id_documento
					LEFT JOIN dbo.rol r ON r.id = u.id_rol
					ORDER BY u.id DESC;";

				using SqlCommand cmd = new(sql, con);

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

				const string sql = @"
					SELECT
						u.id,
						u.nombre_apellido,
						COALESCE(d.numero, '') AS dni,
						COALESCE(u.sexo, '') AS sexo,
						u.telefono,
						COALESCE(u.correo, '') AS correo,
						u.id_rol,
						COALESCE(r.nombre, '') AS rol,
						u.estado
					FROM dbo.usuario u
					LEFT JOIN dbo.documento d ON d.id = u.id_documento
					LEFT JOIN dbo.rol r ON r.id = u.id_rol
					WHERE u.id = @IdUsuario;";

				using SqlCommand cmd = new(sql, con);

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

				const string sql = @"
					DECLARE @IdDocumento INT;
					SELECT @IdDocumento = id
					FROM dbo.documento
					WHERE tipo_documento = 'DNI' AND numero = @DNI;

					IF @IdDocumento IS NULL
					BEGIN
						INSERT INTO dbo.documento(tipo_documento, numero, nombre)
						VALUES ('DNI', @DNI, @NombreApellido);
						SET @IdDocumento = CONVERT(INT, SCOPE_IDENTITY());
					END

					IF @IdUsuario IS NULL
					BEGIN
						INSERT INTO dbo.usuario
							(nombre_apellido, sexo, telefono, correo, contrasena, estado, id_rol, id_documento)
						VALUES
							(@NombreApellido, @Sexo, @Telefono, @Correo, @Contrasena, @Estado, @IdRol, @IdDocumento);
						SET @IdUsuario = CONVERT(INT, SCOPE_IDENTITY());
					END
					ELSE
					BEGIN
						UPDATE dbo.usuario
						SET nombre_apellido = @NombreApellido,
							sexo = @Sexo,
							telefono = @Telefono,
							correo = @Correo,
							contrasena = @Contrasena,
							estado = @Estado,
							id_rol = @IdRol,
							id_documento = @IdDocumento
						WHERE id = @IdUsuario;
					END

					SELECT @IdUsuario AS IdUsuario;";

				using SqlCommand cmd = new(sql, con);
				cmd.Parameters.AddWithValue("@IdUsuario", (object?)modelo.idUsuario ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@NombreApellido", modelo.nombreApellido);
				cmd.Parameters.AddWithValue("@DNI", modelo.dni);
				cmd.Parameters.AddWithValue("@Sexo", string.IsNullOrWhiteSpace(modelo.sexo) ? DBNull.Value : modelo.sexo);
				cmd.Parameters.AddWithValue("@Telefono", (object?)modelo.telefono ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@Correo", modelo.correo);
				cmd.Parameters.AddWithValue("@Contrasena", modelo.contrasena);
				cmd.Parameters.AddWithValue("@IdRol", modelo.idRol);
				cmd.Parameters.AddWithValue("@Estado", modelo.estado);

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
