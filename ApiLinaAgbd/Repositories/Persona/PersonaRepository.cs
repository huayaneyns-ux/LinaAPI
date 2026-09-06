using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Persona;

namespace ApiLinaAgbd.Repositories.Persona
{
	public class PersonaRepository : IPersonaRepository
	{
		private readonly Conexion _conexion;
		private readonly ILogger<PersonaRepository> _logger;

		public PersonaRepository(Conexion conexion, ILogger<PersonaRepository> logger)
		{
			_conexion = conexion;
			_logger = logger;
		}

		public PersonaData? Buscar(string tipoDocumento, string numero)
		{
			try
			{
				using SqlConnection con = _conexion.ObtenerConexion();
				con.Open();
				const string sql = @"
					SELECT TOP 1 d.numero, d.nombre, COALESCE(dir.nombre_direccion, '') AS direccion
					FROM dbo.documento d
					OUTER APPLY (
						SELECT TOP 1 dr.nombre_direccion
						FROM dbo.usuario u
						INNER JOIN dbo.usuario_direccion ud ON ud.id_usuario = u.id AND ud.estado = 1
						INNER JOIN dbo.direccion dr ON dr.id = ud.id_direccion
						WHERE u.id_documento = d.id
						ORDER BY ud.es_principal DESC, ud.id DESC
					) dir
					WHERE d.tipo_documento = @tipoDocumento AND d.numero = @numero;";
				using SqlCommand cmd = new(sql, con);
				cmd.Parameters.AddWithValue("@tipoDocumento", tipoDocumento);
				cmd.Parameters.AddWithValue("@numero", numero);
				using SqlDataReader dr = cmd.ExecuteReader();
				if (!dr.Read()) return null;
				var nombre = dr["nombre"] == DBNull.Value ? "" : dr["nombre"].ToString()?.Trim() ?? "";
				var numeroBd = dr["numero"] == DBNull.Value ? "" : dr["numero"].ToString()?.Trim() ?? "";
				return string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(numeroBd)
					? null
					: new PersonaData { Numero = numeroBd, Nombre = nombre, Direccion = dr["direccion"]?.ToString() };
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al buscar documento con tipo {Tipo} y número {Numero}", tipoDocumento, numero);
				return null;
			}
		}

		public int Registrar(string tipoDocumento, string numero, string nombreApellido, string? direccion, string? ubigeo)
		{
			using SqlConnection con = _conexion.ObtenerConexion();
			con.Open();
			using SqlTransaction transaction = con.BeginTransaction();
			try
			{
				const string sql = @"
					DECLARE @DocumentoId INT, @UsuarioId INT, @DireccionId INT;
					SELECT @DocumentoId = id FROM dbo.documento WITH (UPDLOCK, HOLDLOCK)
					WHERE tipo_documento = @tipoDocumento AND numero = @numero;
					IF @DocumentoId IS NULL
					BEGIN
						INSERT INTO dbo.documento(tipo_documento, numero, nombre)
						VALUES (@tipoDocumento, @numero, @nombre);
						SET @DocumentoId = CONVERT(INT, SCOPE_IDENTITY());
					END;
					SELECT TOP 1 @UsuarioId = id FROM dbo.usuario WITH (UPDLOCK, HOLDLOCK)
					WHERE id_documento = @DocumentoId;
					IF @UsuarioId IS NULL
					BEGIN
						INSERT INTO dbo.usuario(nombre_apellido, sexo, telefono, correo, contrasena, estado, id_rol, id_documento)
						VALUES (@nombre, NULL, NULL, NULL, NULL, 1, 1, @DocumentoId);
						SET @UsuarioId = CONVERT(INT, SCOPE_IDENTITY());
					END;
					IF NULLIF(@direccion, '') IS NOT NULL AND NULLIF(@ubigeo, '') IS NOT NULL
					BEGIN
						SELECT TOP 1 @DireccionId = d.id
						FROM dbo.direccion d INNER JOIN dbo.distrito dist ON dist.id = d.id_distrito
						WHERE d.nombre_direccion = @direccion AND dist.codigo_ubigeo = @ubigeo;
						IF @DireccionId IS NULL
						BEGIN
							INSERT INTO dbo.direccion(nombre_direccion, referencia, id_distrito)
							SELECT @direccion, NULL, id FROM dbo.distrito WHERE codigo_ubigeo = @ubigeo;
							IF @@ROWCOUNT > 0 SET @DireccionId = CONVERT(INT, SCOPE_IDENTITY());
						END;
						IF @DireccionId IS NOT NULL AND EXISTS (
							SELECT 1 FROM dbo.usuario_direccion
							WHERE id_usuario = @UsuarioId AND id_direccion = @DireccionId)
						BEGIN
							UPDATE dbo.usuario_direccion SET estado = 1
							WHERE id_usuario = @UsuarioId AND id_direccion = @DireccionId;
						END
						ELSE IF @DireccionId IS NOT NULL
						BEGIN
							INSERT INTO dbo.usuario_direccion(id_usuario, id_direccion, es_principal, estado)
							VALUES (@UsuarioId, @DireccionId,
								CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.usuario_direccion WHERE id_usuario = @UsuarioId AND estado = 1) THEN 1 ELSE 0 END, 1);
						END;
					END;
					SELECT @UsuarioId;";
				using SqlCommand cmd = new(sql, con, transaction);
				cmd.Parameters.AddWithValue("@tipoDocumento", tipoDocumento);
				cmd.Parameters.AddWithValue("@numero", numero);
				cmd.Parameters.AddWithValue("@nombre", nombreApellido);
				cmd.Parameters.AddWithValue("@direccion", direccion ?? string.Empty);
				cmd.Parameters.AddWithValue("@ubigeo", ubigeo ?? string.Empty);
				var idUsuario = Convert.ToInt32(cmd.ExecuteScalar());
				transaction.Commit();
				return idUsuario;
			}
			catch (Exception ex)
			{
				transaction.Rollback();
				_logger.LogError(ex, "Error al registrar persona {Tipo} {Numero}", tipoDocumento, numero);
				throw;
			}
		}
	}
}
