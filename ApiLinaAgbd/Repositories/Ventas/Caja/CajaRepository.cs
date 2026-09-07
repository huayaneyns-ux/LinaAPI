using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas.Caja;

namespace ApiLinaAgbd.Repositories.Ventas.Caja
{
	public class CajaRepository : ICajaRepository
	{
		private readonly Conexion _conexion;

		public CajaRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public int RegistrarVenta(CajaVentaInsertDto venta)
		{
			int idVenta = 0;
			if (venta.Pagos is null || venta.Pagos.Count != 1)
				throw new ArgumentException("La venta debe tener un único pago en efectivo.");

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();
				using (var metodoCmd = new SqlCommand(
					"SELECT TOP 1 nombre FROM dbo.MetodoPago WHERE id = @IdMetodoPago AND estado = 1;", con))
				{
					metodoCmd.Parameters.AddWithValue("@IdMetodoPago", venta.Pagos[0].IdMetodoPago);
					var nombreMetodo = metodoCmd.ExecuteScalar()?.ToString() ?? string.Empty;
					if (!nombreMetodo.Contains("EFECTIVO", StringComparison.OrdinalIgnoreCase))
						throw new ArgumentException("Solo se permite el método de pago Efectivo.");
				}

				SqlTransaction transaction = con.BeginTransaction();

				try
				{
					SqlCommand cmdVenta =
						new SqlCommand(
							"USP_VTA_INS_VENTA",
							con,
							transaction);

					cmdVenta.CommandType =
						CommandType.StoredProcedure;

					cmdVenta.Parameters.AddWithValue(
						"@IdCliente",
						(object?)venta.IdCliente ?? DBNull.Value);

					cmdVenta.Parameters.AddWithValue(
						"@IdUsuario",
						venta.IdUsuario);

					cmdVenta.Parameters.AddWithValue(
						"@Fecha",
						DateTime.Now);

					cmdVenta.Parameters.AddWithValue(
						"@Estado",
						"Completada");

					cmdVenta.Parameters.AddWithValue(
						"@IGV",
						venta.Igv);

					idVenta = Convert.ToInt32(
						cmdVenta.ExecuteScalar()
					);

					foreach (var item in venta.Detalle)
					{
						SqlCommand cmdDetalle = new SqlCommand(
							"USP_VTA_INS_DETALLE",
							con,
							transaction);

						cmdDetalle.CommandType = CommandType.StoredProcedure;

						cmdDetalle.Parameters.AddWithValue("@IdVenta", idVenta);
						cmdDetalle.Parameters.AddWithValue("@IdProducto", item.IdProducto);
						cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
						cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", item.PrecioUnitario);

						List<(int IdDetalleVenta, int IdLote, int Cantidad)> lotes = new();

						using (SqlDataReader dr = cmdDetalle.ExecuteReader())
						{
							while (dr.Read())
							{
								lotes.Add((
									Convert.ToInt32(dr["IdDetalleVenta"]),
									Convert.ToInt32(dr["IdLote"]),
									Convert.ToInt32(dr["Cantidad"])
								));
							}
						}

						foreach (var lote in lotes)
						{
							SqlCommand cmdDetalleLote = new SqlCommand(
								"USP_VTA_INS_DETALLE_LOTE",
								con,
								transaction);

							cmdDetalleLote.CommandType = CommandType.StoredProcedure;

							cmdDetalleLote.Parameters.AddWithValue("@IdDetalleVenta", lote.IdDetalleVenta);
							cmdDetalleLote.Parameters.AddWithValue("@IdLote", lote.IdLote);
							cmdDetalleLote.Parameters.AddWithValue("@Cantidad", lote.Cantidad);

							cmdDetalleLote.ExecuteNonQuery();

							SqlCommand cmdMovimiento = new SqlCommand(
								"USP_MOV_INS_MOVIMIENTO",
								con,
								transaction);

							cmdMovimiento.CommandType = CommandType.StoredProcedure;

							cmdMovimiento.Parameters.AddWithValue("@IdUsuario", venta.IdUsuario);
							cmdMovimiento.Parameters.AddWithValue("@IdLote", lote.IdLote);
							cmdMovimiento.Parameters.AddWithValue("@IdProducto", item.IdProducto);
							cmdMovimiento.Parameters.AddWithValue("@Tipo", 2);
							cmdMovimiento.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
							cmdMovimiento.Parameters.AddWithValue("@Motivo", $"Venta N° {idVenta}");

							cmdMovimiento.ExecuteNonQuery();
						}
					}

					foreach (var pago in venta.Pagos)
					{
						SqlCommand cmdPago =
							new SqlCommand(
								"USP_VTA_INS_PAGO",
								con,
								transaction);

						cmdPago.CommandType =
							CommandType.StoredProcedure;

						cmdPago.Parameters.AddWithValue(
							"@IdVenta",
							idVenta);

						cmdPago.Parameters.AddWithValue(
							"@IdMetodoPago",
							pago.IdMetodoPago);

						cmdPago.Parameters.AddWithValue(
							"@Monto",
							pago.Monto);

						cmdPago.Parameters.AddWithValue(
							"@CodigoOperacion",
							pago.CodigoOperacion ?? "");

						cmdPago.ExecuteNonQuery();
					}

					transaction.Commit();
				}
				catch
				{
					transaction.Rollback();
					throw;
				}
			}

			return idVenta;
		}

		public CajaClienteDto? BuscarCliente(string dni)
		{
			return BuscarClientePorDocumento("DNI", dni);
		}

		public CajaClienteDto? BuscarClientePorDocumento(string tipoDocumento, string numero)
		{
			using var con = _conexion.ObtenerConexion();
			con.Open();
			const string sql = @"
				SELECT TOP 1 u.id, d.tipo_documento, d.numero, d.nombre,
				       u.telefono, u.correo, COALESCE(dir.nombre_direccion, '') AS direccion
				FROM dbo.usuario u
				INNER JOIN dbo.documento d ON d.id = u.id_documento
				OUTER APPLY (
					SELECT TOP 1 dr.nombre_direccion
					FROM dbo.UsuarioDireccion ud
					INNER JOIN dbo.direccion dr ON dr.id = ud.id_direccion
					WHERE ud.id_usuario = u.id AND ud.estado = 1
					ORDER BY ud.es_principal DESC, ud.id DESC
				) dir
				WHERE d.tipo_documento = @TipoDocumento AND d.numero = @Numero;";
			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
			cmd.Parameters.AddWithValue("@Numero", numero);
			using var dr = cmd.ExecuteReader();
			if (!dr.Read()) return null;
			return new CajaClienteDto
			{
				Id = Convert.ToInt32(dr["id"]),
				TipoDocumento = dr["tipo_documento"]?.ToString() ?? tipoDocumento,
				Documento = dr["numero"]?.ToString() ?? numero,
				DNI = tipoDocumento == "DNI" ? dr["numero"]?.ToString() ?? numero : string.Empty,
				NombreApellido = dr["nombre"]?.ToString() ?? string.Empty,
				Telefono = dr["telefono"]?.ToString() ?? string.Empty,
				Correo = dr["correo"]?.ToString() ?? string.Empty,
				Direccion = dr["direccion"]?.ToString() ?? string.Empty
			};
		}

		public int CrearOReutilizarCliente(CajaClienteInsertDto cliente)
		{
			using var con = _conexion.ObtenerConexion();
			con.Open();
			using var tx = con.BeginTransaction();
			try
			{
				const string sql = @"
				DECLARE @DocumentoId INT;
				DECLARE @UsuarioId INT;
				SELECT @DocumentoId = id
				FROM dbo.documento WITH (UPDLOCK, HOLDLOCK)
				WHERE tipo_documento = @TipoDocumento AND numero = @Numero;
				IF @DocumentoId IS NULL
				BEGIN
					INSERT INTO dbo.documento(tipo_documento, numero, nombre)
					VALUES (@TipoDocumento, @Numero, @NombreApellido);
					SET @DocumentoId = CONVERT(INT, SCOPE_IDENTITY());
				END;

				SELECT TOP 1 @UsuarioId = id
				FROM dbo.usuario WITH (UPDLOCK, HOLDLOCK)
				WHERE id_documento = @DocumentoId;

				IF @UsuarioId IS NULL
				BEGIN
					INSERT INTO dbo.usuario(
						nombre_apellido, telefono, correo, contrasena,
						estado, id_rol, id_documento)
					VALUES (
						@NombreApellido, NULLIF(@Telefono, ''), NULLIF(@Correo, ''), NULL,
						1, 1, @DocumentoId);
					SET @UsuarioId = CONVERT(INT, SCOPE_IDENTITY());
				END;

				IF NULLIF(@Direccion, '') IS NOT NULL AND NULLIF(@Ubigeo, '') IS NOT NULL
				BEGIN
					DECLARE @DireccionId INT;
					SET @DireccionId = NULL;
					IF EXISTS (
						SELECT 1
						FROM dbo.distrito d
						WHERE d.codigo_ubigeo = @Ubigeo
						  AND NOT EXISTS (
							  SELECT 1
							  FROM dbo.UsuarioDireccion ud
							  INNER JOIN dbo.direccion dr ON dr.id = ud.id_direccion
							  WHERE ud.id_usuario = @UsuarioId
							    AND ud.estado = 1
							    AND dr.nombre_direccion = @Direccion
						  )
					)
					BEGIN
						INSERT INTO dbo.direccion(nombre_direccion, referencia, id_distrito)
						SELECT @Direccion, NULL, d.id
						FROM dbo.distrito d
						WHERE d.codigo_ubigeo = @Ubigeo;
						SET @DireccionId = CONVERT(INT, SCOPE_IDENTITY());
					END;
					IF @DireccionId IS NOT NULL
					BEGIN
						INSERT INTO dbo.UsuarioDireccion(id_usuario, id_direccion, es_principal, estado)
						VALUES (@UsuarioId, @DireccionId,
							CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.UsuarioDireccion WHERE id_usuario = @UsuarioId AND estado = 1) THEN 1 ELSE 0 END,
							1, SYSUTCDATETIME());
					END;
				END;

				SELECT @UsuarioId;";
				using var cmd = new SqlCommand(sql, con, tx);
				var numero = string.IsNullOrWhiteSpace(cliente.Documento) ? cliente.DNI : cliente.Documento;
				cmd.Parameters.AddWithValue("@TipoDocumento", cliente.TipoDocumento.ToUpperInvariant());
				cmd.Parameters.AddWithValue("@Numero", numero);
				cmd.Parameters.AddWithValue("@NombreApellido", cliente.NombreApellido);
				cmd.Parameters.AddWithValue("@Telefono", cliente.Telefono ?? "");
				cmd.Parameters.AddWithValue("@Correo", cliente.Correo ?? "");
				cmd.Parameters.AddWithValue("@Direccion", cliente.Direccion ?? "");
				cmd.Parameters.AddWithValue("@Ubigeo", cliente.Ubigeo ?? "");
				var id = Convert.ToInt32(cmd.ExecuteScalar());
				tx.Commit();
				return id;
			}
			catch
			{
				tx.Rollback();
				throw;
			}
		}

		public void RegistrarPago(int id, CajaPagoInsertDto pago)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PRO_INS_PAGO",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@id_venta", id);

				cmd.Parameters.AddWithValue(
					"@id_metodo_pago",
					pago.IdMetodoPago
				);

				cmd.Parameters.AddWithValue(
					"@monto",
					pago.Monto
				);

				cmd.Parameters.AddWithValue(
					"@fecha",
					pago.Fecha
				);

				cmd.Parameters.AddWithValue(
					"@codigo_operacion",
					(object?)pago.CodigoOperacion ?? DBNull.Value
				);

				cmd.ExecuteNonQuery();
			}
		}
	}
}
