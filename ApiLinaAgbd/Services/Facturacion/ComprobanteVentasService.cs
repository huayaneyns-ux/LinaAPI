using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Models.Facturacion.Ubl;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class ComprobanteVentasService
	{
		private const string SerieBoleta = "B001";
		private const string SerieFactura = "F001";
		private const string TipoBoletaSunat = "03";
		private const string TipoFacturaSunat = "01";

		private readonly Conexion _conexion;
		private readonly BoletaUblBuilder _boletaBuilder;
		private readonly FacturaUblBuilder _facturaBuilder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionPdfLocalService _pdfLocalService;
		private readonly FacturacionSettings _settings;

		public ComprobanteVentasService(
			Conexion conexion,
			BoletaUblBuilder boletaBuilder,
			FacturaUblBuilder facturaBuilder,
			FacturacionSunatService facturacionSunatService,
			FacturacionPdfLocalService pdfLocalService,
			Microsoft.Extensions.Options.IOptions<FacturacionSettings> options)
		{
			_conexion = conexion;
			_boletaBuilder = boletaBuilder;
			_facturaBuilder = facturaBuilder;
			_facturacionSunatService = facturacionSunatService;
			_pdfLocalService = pdfLocalService;
			_settings = options.Value;
		}

		public async Task<List<VentaComprobanteDisponibleDto>> ListarVentasDisponiblesAsync()
		{
			var ventas = new Dictionary<int, VentaComprobanteDisponibleDto>();

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = @"
				SELECT
					v.id AS VentaId,
					v.fecha,
					v.igv,
					u.nombre_apellido AS ClienteNombre,
					COALESCE(NULLIF(d.tipo_documento, ''), CASE
						WHEN NULLIF(u.ruc, '') IS NOT NULL THEN 'ruc'
						WHEN NULLIF(u.dni, '') IS NOT NULL THEN 'dni'
						ELSE ''
					END) AS TipoDocumentoCliente,
					COALESCE(NULLIF(d.numero, ''), NULLIF(u.ruc, ''), NULLIF(u.dni, ''), '') AS DocumentoCliente,
					COALESCE(NULLIF(d.nombre, ''), NULLIF(u.nombre_apellido, ''), '') AS NombreDocumentoCliente,
					COALESCE(NULLIF(dir.nombre_direccion, ''), '') AS DireccionCliente,
					COALESCE(NULLIF(u.correo, ''), '') AS CorreoCliente,
					p.id AS ProductoId,
					COALESCE(NULLIF(p.codigo, ''), '') AS CodigoProducto,
					COALESCE(NULLIF(p.descripcion, ''), NULLIF(p.nombre, ''), '') AS DescripcionProducto,
					CAST(dv.cantidad AS decimal(18, 2)) AS Cantidad,
					CAST(dv.preciounitario AS decimal(18, 2)) AS PrecioUnitario,
					COALESCE(NULLIF(um.abreviatura, ''), 'NIU') AS UnidadMedida
				FROM dbo.venta v
				INNER JOIN dbo.usuario u ON u.id = v.id_cliente
				LEFT JOIN dbo.documento d ON d.id = u.id_documento
				OUTER APPLY (
					SELECT TOP 1 dir.nombre_direccion
					FROM dbo.UsuarioDireccion ud
					INNER JOIN dbo.direccion dir ON dir.id = ud.id_direccion
					WHERE ud.id_usuario = u.id
					  AND ud.estado = 1
					ORDER BY ud.es_principal DESC, ud.fecha_registro DESC, ud.id DESC
				) dir
				INNER JOIN dbo.detalleventa dv ON dv.id_venta = v.id
				INNER JOIN dbo.producto p ON p.id = dv.id_producto
				LEFT JOIN dbo.unidadmedida um ON um.id = p.id_unidad_medida
				WHERE NOT EXISTS (
					SELECT 1
					FROM dbo.Voucher vx
					WHERE vx.VentaId = v.id
					  AND vx.SunatTypeCode IN ('01', '03')
					  AND ISNULL(vx.SunatStatus, 'NO_ENVIADO') IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO')
				)
				ORDER BY v.id DESC, dv.id ASC;";

			using var cmd = new SqlCommand(sql, con) { CommandType = CommandType.Text };
			using var dr = await cmd.ExecuteReaderAsync();

			while (await dr.ReadAsync())
			{
				var ventaId = dr.GetInt32(dr.GetOrdinal("VentaId"));
				if (!ventas.TryGetValue(ventaId, out var venta))
				{
					var fecha = dr.GetDateTime(dr.GetOrdinal("fecha"));
					var tipoDocumento = MapearTipoDocumentoUi(dr["TipoDocumentoCliente"]?.ToString());
					var documento = dr["DocumentoCliente"]?.ToString() ?? string.Empty;
					var nombreDocumento = dr["NombreDocumentoCliente"]?.ToString() ?? string.Empty;
					var nombreCliente = dr["ClienteNombre"]?.ToString() ?? string.Empty;

					venta = new VentaComprobanteDisponibleDto
					{
						Id = ventaId.ToString(),
						Codigo = $"VTA-{ventaId:D6}",
						Fecha = fecha.ToString("yyyy-MM-dd"),
						Cliente = new ComprobanteVentaClienteDto
						{
							TipoDocumento = string.IsNullOrWhiteSpace(tipoDocumento) ? "DNI" : tipoDocumento,
							Documento = documento,
							Nombre = string.IsNullOrWhiteSpace(nombreDocumento) ? nombreCliente : nombreDocumento,
							Direccion = dr["DireccionCliente"]?.ToString() ?? string.Empty,
							Correo = dr["CorreoCliente"]?.ToString() ?? string.Empty
						}
					};

					ventas.Add(ventaId, venta);
				}

				var cantidad = Convert.ToDecimal(dr["Cantidad"]);
				var precio = Convert.ToDecimal(dr["PrecioUnitario"]);
				var porcentajeIgv = ObtenerIgvVenta(dr);
				var subtotal = Redondear(cantidad * precio);
				var igvItem = Redondear(subtotal * porcentajeIgv / 100m);

				venta.Detalle.Add(new VentaComprobanteDetalleDto
				{
					ProductoId = dr.GetInt32(dr.GetOrdinal("ProductoId")),
					Codigo = dr["CodigoProducto"]?.ToString() ?? string.Empty,
					ProductoServicio = dr["DescripcionProducto"]?.ToString() ?? string.Empty,
					Cantidad = cantidad,
					Precio = precio,
					Igv = igvItem,
					Importe = Redondear(subtotal + igvItem),
					UnidadMedida = dr["UnidadMedida"]?.ToString() ?? "NIU"
				});
			}

			foreach (var venta in ventas.Values)
			{
				venta.Subtotal = Redondear(venta.Detalle.Sum(x => x.Precio * x.Cantidad));
				venta.Igv = Redondear(venta.Detalle.Sum(x => x.Igv));
				venta.Total = Redondear(venta.Subtotal + venta.Igv);
			}

			return ventas.Values.ToList();
		}

		public async Task<List<ComprobanteVentaListItemDto>> ListarComprobantesAsync()
		{
			var vouchers = new Dictionary<string, ComprobanteVentaListItemDto>(StringComparer.OrdinalIgnoreCase);

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					v.Id,
					v.VentaId,
					v.SunatTypeCode,
					v.Series,
					v.Number,
					v.IssueDate,
					v.DueDate,
					v.Currency,
					v.PaymentCondition,
					v.Subtotal,
					v.Igv,
					v.Total,
					v.SunatStatus,
					v.SunatDocumentId,
					v.XmlUrl,
					v.CdrUrl,
					v.PdfA4Url,
					v.PdfA5Url,
					v.Pdf58mmUrl,
					v.Pdf80mmUrl,
					v.CreatedAt,
					v.UpdatedAt,
					COALESCE(vp.DocumentType, '') AS ClienteTipoDocumento,
					COALESCE(vp.DocumentNumber, '') AS ClienteDocumento,
					COALESCE(vp.Name, '') AS ClienteNombre,
					COALESCE(vp.Address, '') AS ClienteDireccion,
					COALESCE(vo.Observation, '') AS Observation,
					COALESCE(vi.Id, '00000000-0000-0000-0000-000000000000') AS ItemId,
					COALESCE(vi.ProductId, 0) AS ProductId,
					COALESCE(vi.ProductCode, '') AS ProductCode,
					COALESCE(vi.Description, '') AS ItemDescription,
					COALESCE(vi.Quantity, 0) AS ItemQuantity,
					COALESCE(vi.UnitPrice, 0) AS ItemUnitPrice,
					COALESCE(vi.Igv, 0) AS ItemIgv,
					COALESCE(vi.Total, 0) AS ItemTotal,
					COALESCE(vi.UnitCode, 'NIU') AS ItemUnitCode,
					COALESCE(vi.LineNumber, 0) AS LineNumber,
					COALESCE(inst.Id, '00000000-0000-0000-0000-000000000000') AS InstallmentId,
					COALESCE(inst.InstallmentNumber, 0) AS InstallmentNumber,
					COALESCE(inst.Amount, 0) AS InstallmentAmount,
					inst.DueDate AS InstallmentDueDate,
					COALESCE(st.OperationType, '') AS LastOperationType,
					COALESCE(st.TransmissionStatus, '') AS LastTransmissionStatus,
					COALESCE(st.HttpStatus, 0) AS LastHttpStatus,
					COALESCE(st.ErrorMessage, '') AS LastErrorMessage,
					st.RespondedAt AS LastRespondedAt
				FROM dbo.Voucher v
				LEFT JOIN dbo.VoucherParty vp
					ON vp.VoucherId = v.Id
				   AND vp.Role = 'CUSTOMER'
				LEFT JOIN dbo.VoucherObservation vo
					ON vo.VoucherId = v.Id
				LEFT JOIN dbo.VoucherItem vi
					ON vi.VoucherId = v.Id
				LEFT JOIN dbo.VoucherInstallment inst
					ON inst.VoucherId = v.Id
				OUTER APPLY (
					SELECT TOP 1
						t.OperationType,
						t.TransmissionStatus,
						t.HttpStatus,
						t.ErrorMessage,
						t.RespondedAt
					FROM dbo.SunatTransmission t
					WHERE t.VoucherId = v.Id
					ORDER BY t.CreatedAt DESC, t.AttemptNumber DESC
				) st
				WHERE v.SunatTypeCode IN ('01', '03')
				ORDER BY v.CreatedAt DESC, vi.LineNumber ASC, inst.InstallmentNumber ASC, vo.LineNumber ASC;
				""";

			using var cmd = new SqlCommand(sql, con) { CommandType = CommandType.Text };
			using var dr = await cmd.ExecuteReaderAsync();

			while (await dr.ReadAsync())
			{
				var id = dr["Id"].ToString() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				if (!vouchers.TryGetValue(id, out var voucher))
				{
					var paymentCondition = dr["PaymentCondition"]?.ToString() ?? string.Empty;
					voucher = new ComprobanteVentaListItemDto
					{
						Id = id,
						Tipo = dr["SunatTypeCode"]?.ToString() == TipoFacturaSunat ? "FACTURA" : "BOLETA",
						Serie = dr["Series"]?.ToString() ?? string.Empty,
						Numero = dr["Number"]?.ToString() ?? string.Empty,
						FechaEmision = Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
						Cliente = dr["ClienteNombre"]?.ToString() ?? string.Empty,
						DocumentoCliente = dr["ClienteDocumento"]?.ToString() ?? string.Empty,
						Subtotal = dr["Subtotal"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Subtotal"]),
						Igv = dr["Igv"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Igv"]),
						Total = dr["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Total"]),
						Estado = MapearEstadoUi(dr["LastOperationType"]?.ToString(), dr["LastTransmissionStatus"]?.ToString(), dr["SunatStatus"]?.ToString()),
						EstadoSunat = MapearEstadoSunatUi(dr["SunatStatus"]?.ToString()),
						TipoDocumentoCliente = MapearTipoDocumentoUi(dr["ClienteTipoDocumento"]?.ToString()),
						DireccionCliente = dr["ClienteDireccion"]?.ToString() ?? string.Empty,
						CorreoCliente = string.Empty,
						CodigoRespuestaSunat = dr["LastHttpStatus"] == DBNull.Value || Convert.ToInt32(dr["LastHttpStatus"]) == 0
							? string.Empty
							: Convert.ToInt32(dr["LastHttpStatus"]).ToString(CultureInfo.InvariantCulture),
						MensajeSunat = dr["LastErrorMessage"]?.ToString() ?? string.Empty,
						FechaConsultaSunat = dr["LastRespondedAt"] == DBNull.Value
							? Convert.ToDateTime(dr["UpdatedAt"]).ToString("s")
							: Convert.ToDateTime(dr["LastRespondedAt"]).ToString("s"),
						FechaEnvioSunat = Convert.ToDateTime(dr["UpdatedAt"]).ToString("s"),
						DocumentId = dr["SunatDocumentId"]?.ToString(),
						FileName = $"{_settings.Emisor.Ruc}-{dr["SunatTypeCode"]}-{dr["Series"]}-{dr["Number"]}",
						PdfUrl = dr["PdfA4Url"] == DBNull.Value ? null : dr["PdfA4Url"].ToString(),
						XmlUrl = dr["XmlUrl"] == DBNull.Value ? null : dr["XmlUrl"].ToString(),
						CdrUrl = dr["CdrUrl"] == DBNull.Value ? null : dr["CdrUrl"].ToString(),
						VentaOrigenId = dr["VentaId"] == DBNull.Value ? null : dr["VentaId"].ToString(),
						FechaVencimiento = dr["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(dr["DueDate"]).ToString("yyyy-MM-dd"),
						Observaciones = null,
						Pago = string.IsNullOrWhiteSpace(paymentCondition)
							? null
							: new ComprobanteVentaPagoDto
							{
								FormaPago = paymentCondition,
								Cuotas = new List<ComprobanteVentaCuotaDto>()
							}
					};

					vouchers.Add(id, voucher);
				}

				var observation = dr["Observation"]?.ToString();
				if (!string.IsNullOrWhiteSpace(observation))
				{
					voucher.Observaciones = string.IsNullOrWhiteSpace(voucher.Observaciones)
						? observation
						: $"{voucher.Observaciones} # {observation}";
				}

				var itemId = dr["ItemId"]?.ToString();
				if (!string.IsNullOrWhiteSpace(itemId) &&
					!string.Equals(itemId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase) &&
					!voucher.Detalle.Any(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase)))
				{
					var cantidad = Convert.ToDecimal(dr["ItemQuantity"]);
					var precio = Convert.ToDecimal(dr["ItemUnitPrice"]);
					var igv = Convert.ToDecimal(dr["ItemIgv"]);
					var importe = dr["ItemTotal"] == DBNull.Value ? Redondear((cantidad * precio) + igv) : Convert.ToDecimal(dr["ItemTotal"]);

					voucher.Detalle.Add(new VentaComprobanteDetalleDto
					{
						ItemId = itemId,
						ProductoId = Convert.ToInt32(dr["ProductId"]) <= 0 ? null : Convert.ToInt32(dr["ProductId"]),
						Codigo = dr["ProductCode"]?.ToString() ?? string.Empty,
						ProductoServicio = dr["ItemDescription"]?.ToString() ?? string.Empty,
						Cantidad = cantidad,
						Precio = precio,
						Igv = igv,
						Importe = importe,
						UnidadMedida = dr["ItemUnitCode"]?.ToString() ?? "NIU"
					});
				}

				var installmentId = dr["InstallmentId"]?.ToString();
				if (!string.IsNullOrWhiteSpace(installmentId) &&
					!string.Equals(installmentId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase) &&
					voucher.Pago is not null &&
					!voucher.Pago.Cuotas.Any(x =>
						x.Numero == Convert.ToInt32(dr["InstallmentNumber"])))
				{
					voucher.Pago.Cuotas.Add(new ComprobanteVentaCuotaDto
					{
						Numero = Convert.ToInt32(dr["InstallmentNumber"]),
						Monto = Convert.ToDecimal(dr["InstallmentAmount"]),
						FechaVencimiento = dr["InstallmentDueDate"] == DBNull.Value
							? string.Empty
							: Convert.ToDateTime(dr["InstallmentDueDate"]).ToString("yyyy-MM-dd")
					});
				}
			}

			foreach (var voucher in vouchers.Values)
			{
				if (voucher.Pago is not null)
				{
					voucher.Pago.Cuotas = voucher.Pago.Cuotas
						.OrderBy(x => x.Numero)
						.ToList();
				}
			}

			return vouchers.Values.ToList();
		}

		public async Task<ComprobanteVentaListItemDto> ObtenerComprobantePorIdAsync(string id)
		{
			if (!Guid.TryParse(id, out var voucherId))
			{
				throw new InvalidOperationException("El identificador del voucher no es válido.");
			}

			return (await ListarComprobantesAsync()).FirstOrDefault(x => string.Equals(x.Id, voucherId.ToString(), StringComparison.OrdinalIgnoreCase))
				?? throw new InvalidOperationException("No se encontró el comprobante seleccionado.");
		}

		public async Task<ComprobanteVentaListItemDto> SincronizarEstadoSunatAsync(string id)
		{
			var voucher = await ObtenerComprobantePorIdAsync(id);
			if (EsAnulacionConfirmada(voucher.Estado, voucher.EstadoSunat))
			{
				throw new InvalidOperationException("El comprobante ya fue anulado y confirmado por SUNAT. No se puede actualizar nuevamente.");
			}
			if (string.IsNullOrWhiteSpace(voucher.DocumentId))
			{
				throw new InvalidOperationException("El comprobante no tiene documentId registrado en APISUNAT.");
			}

			var consultaInicioUtc = DateTime.UtcNow;
			var consulta = await _facturacionSunatService.ObtenerDocumentoPorId(voucher.DocumentId);
			if (!consulta.Exitoso)
			{
				throw new InvalidOperationException(consulta.DetalleError ?? consulta.MensajeSunat ?? consulta.Mensaje);
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();
			await ActualizarVoucherPostConsultaAsync(con, id, consulta);
			await RegistrarTransmisionAsync(con, id, "STATUS_QUERY", consulta, consultaInicioUtc);

			return await ObtenerComprobantePorIdAsync(id);
		}

		public async Task<(byte[] Content, string ContentType, string FileName)> DescargarPdfAsync(string id, string format)
		{
			var formato = (format ?? string.Empty).Trim();
			if (formato is not ("A4" or "A5" or "ticket58mm" or "ticket80mm"))
			{
				throw new InvalidOperationException("El formato PDF solicitado no es válido.");
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					v.SunatTypeCode,
					v.Series,
					v.Number,
					v.PdfA4Url,
					v.PdfA5Url,
					v.Pdf58mmUrl,
					v.Pdf80mmUrl
				FROM dbo.Voucher v
				WHERE v.Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", Guid.Parse(id));
			using var dr = await cmd.ExecuteReaderAsync();
			if (!await dr.ReadAsync())
			{
				throw new InvalidOperationException("No se encontró el comprobante solicitado.");
			}

			var fileName = $"{_settings.Emisor.Ruc}-{dr["SunatTypeCode"]}-{dr["Series"]}-{dr["Number"]}";
			var storedUrl = formato switch
			{
				"A4" => dr["PdfA4Url"] == DBNull.Value ? null : dr["PdfA4Url"].ToString(),
				"A5" => dr["PdfA5Url"] == DBNull.Value ? null : dr["PdfA5Url"].ToString(),
				"ticket58mm" => dr["Pdf58mmUrl"] == DBNull.Value ? null : dr["Pdf58mmUrl"].ToString(),
				"ticket80mm" => dr["Pdf80mmUrl"] == DBNull.Value ? null : dr["Pdf80mmUrl"].ToString(),
				_ => null
			};

			return await _pdfLocalService.LeerPdfLocalAsync(storedUrl, fileName);
		}

		public async Task<ComprobanteVentaListItemDto> AnularAsync(string id, string reason)
		{
			var voucher = await ObtenerComprobantePorIdAsync(id);
			if (EsAnulacionConfirmada(voucher.Estado, voucher.EstadoSunat))
			{
				throw new InvalidOperationException("El comprobante ya fue anulado y confirmado por SUNAT.");
			}
			if (string.IsNullOrWhiteSpace(voucher.DocumentId))
			{
				throw new InvalidOperationException("El comprobante no tiene documentId registrado en APISUNAT.");
			}

			var motivo = (reason ?? string.Empty).Trim();
			if (motivo.Length < 3 || motivo.Length > 100)
			{
				throw new InvalidOperationException("El motivo de anulación debe tener entre 3 y 100 caracteres.");
			}

			var anuladoInicioUtc = DateTime.UtcNow;
			var resultado = await _facturacionSunatService.AnularDocumento(voucher.DocumentId, motivo);
			if (!resultado.Exitoso)
			{
				throw new InvalidOperationException(resultado.DetalleError ?? resultado.MensajeSunat ?? resultado.Mensaje);
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();
			await ActualizarVoucherPostAnulacionAsync(con, id, resultado);
			await RegistrarTransmisionAsync(con, id, "VOID", resultado, anuladoInicioUtc);

			return await ObtenerComprobantePorIdAsync(id);
		}

		public async Task<ComprobanteVentaListItemDto> EmitirAsync(ComprobanteVentaEmitirRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(_settings.Emisor?.Ruc) || string.IsNullOrWhiteSpace(_settings.Emisor.RazonSocial))
			{
				throw new InvalidOperationException("Falta FacturacionSettings:Emisor:Ruc o RazonSocial.");
			}

			var tipo = (request.Tipo ?? string.Empty).Trim().ToUpperInvariant();
			if (tipo is not ("BOLETA" or "FACTURA"))
			{
				throw new InvalidOperationException("Solo se permite emitir boleta o factura desde este módulo.");
			}

			var venta = await ObtenerVentaAsync(request.VentaOrigenId);
			var clienteFiscal = ResolverClienteFiscal(venta, tipo, request.ReceptorSource, request.Cliente);

			var fechaEmision = ParsearFechaObligatoria(request.FechaEmision, "La fecha de emisión es obligatoria.");
			var fechaVencimiento = string.IsNullOrWhiteSpace(request.FechaVencimiento)
				? (DateTime?)null
				: ParsearFechaObligatoria(request.FechaVencimiento, "La fecha de vencimiento no es válida.");
			var moneda = (request.Moneda ?? "PEN").Trim().ToUpperInvariant();
			var pagoNormalizado = NormalizarPago(request.Pago);

			ValidarSolicitud(tipo, venta, clienteFiscal, fechaEmision, fechaVencimiento, moneda, pagoNormalizado, request.ReceptorSource);

			var serie = tipo == "FACTURA" ? SerieFactura : SerieBoleta;
			var tipoComprobanteSunat = tipo == "FACTURA" ? TipoFacturaSunat : TipoBoletaSunat;
			var horaEmision = DateTime.Now.ToString("HH:mm:ss");
			var voucherId = Guid.NewGuid();
			string numero;

			using (var con = _conexion.ObtenerConexion())
			{
				await con.OpenAsync();
				using var tx = con.BeginTransaction();

				await ValidarVentaSinComprobanteAsync(con, tx, request.VentaOrigenId);
				numero = await GenerarNumeroAleatorioDisponibleAsync(con, tx, tipoComprobanteSunat, serie);

				await InsertarVoucherPendienteAsync(con, tx, voucherId, request.VentaOrigenId, tipoComprobanteSunat, serie, numero, fechaEmision, fechaVencimiento, moneda, venta, pagoNormalizado);
				if (DebePersistirClienteSnapshot(tipo, request.ReceptorSource, clienteFiscal))
				{
					await InsertarVoucherPartyAsync(con, tx, voucherId, clienteFiscal);
				}
				await InsertarVoucherItemsAsync(con, tx, voucherId, venta.Detalle);
				await InsertarVoucherObservationsAsync(con, tx, voucherId, request.Observaciones);
				await InsertarVoucherInstallmentsAsync(con, tx, voucherId, pagoNormalizado);
				tx.Commit();
			}

			var boletaRequest = tipo == "BOLETA" ? CrearBoletaRequest(serie, numero, fechaEmision, horaEmision, moneda, venta, clienteFiscal) : null;
			var facturaRequest = tipo == "FACTURA" ? CrearFacturaRequest(serie, numero, fechaEmision, fechaVencimiento, horaEmision, moneda, venta, clienteFiscal, pagoNormalizado) : null;
			var documentBody = tipo == "BOLETA"
				? _boletaBuilder.Build(boletaRequest!)
				: _facturaBuilder.Build(facturaRequest!);
			var fileName = $"{_settings.Emisor.Ruc}-{tipoComprobanteSunat}-{serie}-{numero}";
			var solicitudUtc = DateTime.UtcNow;
			var envio = await _facturacionSunatService.EnviarDocumento(fileName, documentBody);

			if (!FacturacionVoucherHelper.FueRecibidoPorApi(envio))
			{
				using var conLimpieza = _conexion.ObtenerConexion();
				await conLimpieza.OpenAsync();
				await FacturacionVoucherHelper.EliminarVoucherAsync(conLimpieza, voucherId);
				throw new InvalidOperationException(envio.DetalleError ?? envio.MensajeSunat ?? envio.Mensaje);
			}

			using (var con = _conexion.ObtenerConexion())
			{
				await con.OpenAsync();
				await ActualizarVoucherPostEnvioAsync(con, voucherId, envio);
				await RegistrarTransmisionAsync(con, voucherId.ToString(), "SEND", envio, solicitudUtc);
			}

			return await ObtenerComprobantePorIdAsync(voucherId.ToString());
		}

		private async Task<VentaComprobanteDisponibleDto> ObtenerVentaAsync(int ventaId)
		{
			var ventas = await ListarVentasDisponiblesAsync();
			var venta = ventas.FirstOrDefault(x => x.Id == ventaId.ToString());
			if (venta is null)
			{
				throw new InvalidOperationException("La venta seleccionada no existe.");
			}

			if (venta.Detalle.Count == 0)
			{
				throw new InvalidOperationException("La venta seleccionada no tiene detalle para emitir comprobante.");
			}

			return venta;
		}

		private static string GenerarNumeroAleatorio()
		{
			return Random.Shared.Next(0, 100_000_000).ToString("D8");
		}

		private static async Task ValidarVentaSinComprobanteAsync(SqlConnection con, SqlTransaction tx, int ventaId)
		{
			const string sql = """
				SELECT TOP 1
					Series,
					Number,
					SunatTypeCode
				FROM dbo.Voucher
				WHERE VentaId = @VentaId
				  AND SunatTypeCode IN ('01', '03')
				  AND ISNULL(SunatStatus, 'NO_ENVIADO') IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO');
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@VentaId", ventaId);

			using var dr = await cmd.ExecuteReaderAsync();
			if (await dr.ReadAsync())
			{
				var serie = dr["Series"]?.ToString() ?? string.Empty;
				var number = dr["Number"]?.ToString() ?? string.Empty;
				var tipo = (dr["SunatTypeCode"]?.ToString() ?? string.Empty) switch
				{
					"01" => "factura",
					"03" => "boleta",
					_ => "comprobante"
				};

				throw new InvalidOperationException($"La venta ya tiene un {tipo} emitido: {serie}-{number}.");
			}
		}

		private async Task<string> GenerarNumeroAleatorioDisponibleAsync(SqlConnection con, SqlTransaction tx, string tipoComprobanteSunat, string serie)
		{
			const string sql = """
				SELECT COUNT(1)
				FROM dbo.Voucher
				WHERE SunatTypeCode = @tipo
				  AND Series = @serie
				  AND IssuerRuc = @issuerRuc
				  AND Number = @number;
				""";

			for (var intento = 0; intento < 100; intento++)
			{
				var numero = GenerarNumeroAleatorio();
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@tipo", tipoComprobanteSunat);
				cmd.Parameters.AddWithValue("@serie", serie);
				cmd.Parameters.AddWithValue("@issuerRuc", _settings.Emisor.Ruc);
				cmd.Parameters.AddWithValue("@number", numero);

				var existe = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
				if (!existe)
				{
					return numero;
				}
			}

			throw new InvalidOperationException("No se pudo generar un número aleatorio único para el comprobante.");
		}

		private async Task InsertarVoucherPendienteAsync(
			SqlConnection con,
			SqlTransaction tx,
			Guid voucherId,
			int ventaOrigenId,
			string tipoComprobanteSunat,
			string serie,
			string numero,
			DateTime fechaEmision,
			DateTime? fechaVencimiento,
			string moneda,
			VentaComprobanteDisponibleDto venta,
			ComprobanteVentaPagoDto? pago)
		{
			const string sql = """
				INSERT INTO dbo.Voucher
				(
					Id,
					VentaId,
					SunatTypeCode,
					Series,
					Number,
					IssuerRuc,
					IssuerLegalName,
					IssueDate,
					DueDate,
					Currency,
					PaymentCondition,
					Subtotal,
					Igv,
					Total,
					SunatStatus
				)
				VALUES
				(
					@Id,
					@VentaId,
					@SunatTypeCode,
					@Series,
					@Number,
					@IssuerRuc,
					@IssuerLegalName,
					@IssueDate,
					@DueDate,
					@Currency,
					@PaymentCondition,
					@Subtotal,
					@Igv,
					@Total,
					CASE
						WHEN @SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN @SunatStatus
						ELSE 'NO_ENVIADO'
					END
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.AddWithValue("@VentaId", ventaOrigenId);
			cmd.Parameters.AddWithValue("@SunatTypeCode", tipoComprobanteSunat);
			cmd.Parameters.AddWithValue("@Series", serie);
			cmd.Parameters.AddWithValue("@Number", numero);
			cmd.Parameters.AddWithValue("@IssuerRuc", _settings.Emisor.Ruc);
			cmd.Parameters.AddWithValue("@IssuerLegalName", _settings.Emisor.RazonSocial);
			cmd.Parameters.AddWithValue("@IssueDate", fechaEmision.Date);
			cmd.Parameters.AddWithValue("@DueDate", (object?)fechaVencimiento?.Date ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Currency", moneda);
			cmd.Parameters.AddWithValue("@PaymentCondition", (object?)pago?.FormaPago ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Subtotal", venta.Subtotal);
			cmd.Parameters.AddWithValue("@Igv", venta.Igv);
			cmd.Parameters.AddWithValue("@Total", venta.Total);
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = "NO_ENVIADO";
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task InsertarVoucherPartyAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, ComprobanteVentaClienteDto cliente)
		{
			const string sql = """
				INSERT INTO dbo.VoucherParty
				(
					Id,
					VoucherId,
					Role,
					DocumentType,
					DocumentNumber,
					Name,
					Address
				)
				VALUES
				(
					@Id,
					@VoucherId,
					'CUSTOMER',
					@DocumentType,
					@DocumentNumber,
					@Name,
					@Address
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@DocumentType", string.IsNullOrWhiteSpace(cliente.TipoDocumento) ? DBNull.Value : cliente.TipoDocumento);
			cmd.Parameters.AddWithValue("@DocumentNumber", string.IsNullOrWhiteSpace(cliente.Documento) ? DBNull.Value : cliente.Documento);
			cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(cliente.Nombre) ? DBNull.Value : cliente.Nombre);
			cmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(cliente.Direccion) ? DBNull.Value : cliente.Direccion);
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task InsertarVoucherItemsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, List<VentaComprobanteDetalleDto> detalle)
		{
			const string sql = """
				INSERT INTO dbo.VoucherItem
				(
					Id,
					VoucherId,
					LineNumber,
					ProductId,
					ProductCode,
					Description,
					Quantity,
					UnitCode,
					UnitPrice,
					SaleValue,
					IgvPercentage,
					Igv,
					Total
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@LineNumber,
					@ProductId,
					@ProductCode,
					@Description,
					@Quantity,
					@UnitCode,
					@UnitPrice,
					@SaleValue,
					@IgvPercentage,
					@Igv,
					@Total
				);
				""";

			for (var i = 0; i < detalle.Count; i++)
			{
				var item = detalle[i];
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@LineNumber", i + 1);
				cmd.Parameters.AddWithValue("@ProductId", (object?)item.ProductoId ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@ProductCode", string.IsNullOrWhiteSpace(item.Codigo) ? DBNull.Value : item.Codigo);
				cmd.Parameters.AddWithValue("@Description", item.ProductoServicio);
				cmd.Parameters.AddWithValue("@Quantity", item.Cantidad);
				cmd.Parameters.AddWithValue("@UnitCode", string.IsNullOrWhiteSpace(item.UnidadMedida) ? "NIU" : item.UnidadMedida);
				cmd.Parameters.AddWithValue("@UnitPrice", item.Precio);
				cmd.Parameters.AddWithValue("@SaleValue", Redondear(item.Cantidad * item.Precio));
				cmd.Parameters.AddWithValue("@IgvPercentage", ObtenerPorcentajeIgv(item));
				cmd.Parameters.AddWithValue("@Igv", item.Igv);
				cmd.Parameters.AddWithValue("@Total", item.Importe);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private static async Task InsertarVoucherObservationsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, string? observaciones)
		{
			var lineas = SepararObservaciones(observaciones);
			if (lineas.Count == 0)
			{
				return;
			}

			const string sql = """
				INSERT INTO dbo.VoucherObservation
				(
					Id,
					VoucherId,
					LineNumber,
					Observation
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@LineNumber,
					@Observation
				);
				""";

			for (var i = 0; i < lineas.Count; i++)
			{
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@LineNumber", i + 1);
				cmd.Parameters.AddWithValue("@Observation", lineas[i]);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private static async Task InsertarVoucherInstallmentsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, ComprobanteVentaPagoDto? pago)
		{
			if (pago is null || !string.Equals(pago.FormaPago, "CREDITO", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			const string sql = """
				INSERT INTO dbo.VoucherInstallment
				(
					Id,
					VoucherId,
					InstallmentNumber,
					Amount,
					DueDate
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@InstallmentNumber,
					@Amount,
					@DueDate
				);
				""";

			for (var i = 0; i < pago.Cuotas.Count; i++)
			{
				var cuota = pago.Cuotas[i];
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@InstallmentNumber", i + 1);
				cmd.Parameters.AddWithValue("@Amount", cuota.Monto);
				cmd.Parameters.AddWithValue("@DueDate", ParsearFechaObligatoria(cuota.FechaVencimiento, "La fecha de vencimiento de la cuota no es válida."));
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private async Task ActualizarVoucherPostEnvioAsync(SqlConnection con, Guid voucherId, FacturacionEnvioResultado envio)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = CASE
						WHEN @SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN @SunatStatus
						ELSE 'EXCEPCION'
					END,
					SunatDocumentId = @SunatDocumentId,
					XmlUrl = @XmlUrl,
					CdrUrl = @CdrUrl,
					PdfA4Url = @PdfA4Url,
					PdfA5Url = @PdfA5Url,
					Pdf58mmUrl = @Pdf58mmUrl,
					Pdf80mmUrl = @Pdf80mmUrl,
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			var urlsPdf = ExtraerUrlsPdf(envio.RespuestaApi);
			var urlsPdfLocales = await _pdfLocalService.GuardarDesdeUrlsAsync(voucherId, urlsPdf);
			var sunatStatus = NormalizarSunatStatusParaVoucher(envio);
			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = sunatStatus;
			cmd.Parameters.AddWithValue("@SunatDocumentId", (object?)envio.DocumentId ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@XmlUrl", (object?)envio.XmlUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@CdrUrl", (object?)envio.CdrUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@PdfA4Url", (object?)urlsPdfLocales.A4 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@PdfA5Url", (object?)urlsPdfLocales.A5 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Pdf58mmUrl", (object?)urlsPdfLocales.Ticket58 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Pdf80mmUrl", (object?)urlsPdfLocales.Ticket80 ?? DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		private async Task ActualizarVoucherPostConsultaAsync(SqlConnection con, string voucherId, FacturacionEnvioResultado consulta)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = CASE
						WHEN @SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN @SunatStatus
						ELSE 'EXCEPCION'
					END,
					SunatDocumentId = COALESCE(@SunatDocumentId, SunatDocumentId),
					XmlUrl = COALESCE(@XmlUrl, XmlUrl),
					CdrUrl = COALESCE(@CdrUrl, CdrUrl),
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			var sunatStatus = NormalizarSunatStatusParaVoucher(consulta);
			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", Guid.Parse(voucherId));
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = sunatStatus;
			cmd.Parameters.AddWithValue("@SunatDocumentId", (object?)consulta.DocumentId ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@XmlUrl", (object?)consulta.XmlUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@CdrUrl", (object?)consulta.CdrUrl ?? DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		private async Task ActualizarVoucherPostAnulacionAsync(SqlConnection con, string voucherId, FacturacionEnvioResultado resultado)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = 'ANULADO',
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", Guid.Parse(voucherId));
			await cmd.ExecuteNonQueryAsync();
		}

		private async Task RegistrarTransmisionAsync(
			SqlConnection con,
			string voucherId,
			string operationType,
			FacturacionEnvioResultado resultado,
			DateTime createdAtUtc)
			=> await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, Guid.Parse(voucherId), operationType, resultado, createdAtUtc);

		private static ComprobanteVentaClienteDto ResolverClienteFiscal(
			VentaComprobanteDisponibleDto venta,
			string tipo,
			string? receptorSource,
			ComprobanteVentaClienteDto? clienteRequest)
		{
			var source = (receptorSource ?? "SALE_CUSTOMER").Trim().ToUpperInvariant();
			var clienteCustomInformado = TieneDatosCliente(clienteRequest);

			if (source == "SALE_CUSTOMER" && clienteCustomInformado)
			{
				source = "CUSTOM";
			}

			if (source == "SALE_CUSTOMER")
			{
				return ClonarCliente(venta.Cliente);
			}

			if (source == "CUSTOM")
			{
				if (clienteRequest is null)
				{
					throw new InvalidOperationException("Debe enviar los datos del receptor cuando el origen es CUSTOM.");
				}

				return new ComprobanteVentaClienteDto
				{
					TipoDocumento = (clienteRequest.TipoDocumento ?? string.Empty).Trim().ToUpperInvariant(),
					Documento = (clienteRequest.Documento ?? string.Empty).Trim(),
					Nombre = (clienteRequest.Nombre ?? string.Empty).Trim(),
					Direccion = (clienteRequest.Direccion ?? string.Empty).Trim(),
					Correo = (clienteRequest.Correo ?? string.Empty).Trim()
				};
			}

			if (tipo == "BOLETA" && source == "UNIDENTIFIED")
			{
				return new ComprobanteVentaClienteDto();
			}

			throw new InvalidOperationException("El origen del receptor no es válido.");
		}

		private static bool TieneDatosCliente(ComprobanteVentaClienteDto? cliente) =>
			cliente is not null &&
			(!string.IsNullOrWhiteSpace(cliente.TipoDocumento) ||
			 !string.IsNullOrWhiteSpace(cliente.Documento) ||
			 !string.IsNullOrWhiteSpace(cliente.Nombre) ||
			 !string.IsNullOrWhiteSpace(cliente.Direccion) ||
			 !string.IsNullOrWhiteSpace(cliente.Correo));

		private static ComprobanteVentaClienteDto ClonarCliente(ComprobanteVentaClienteDto cliente) =>
			new()
			{
				TipoDocumento = cliente.TipoDocumento,
				Documento = cliente.Documento,
				Nombre = cliente.Nombre,
				Direccion = cliente.Direccion,
				Correo = cliente.Correo
			};

		private static bool DebePersistirClienteSnapshot(string tipo, string? receptorSource, ComprobanteVentaClienteDto cliente) =>
			tipo == "FACTURA" ||
			!string.Equals((receptorSource ?? "SALE_CUSTOMER").Trim(), "UNIDENTIFIED", StringComparison.OrdinalIgnoreCase) &&
			(!string.IsNullOrWhiteSpace(cliente.Documento) || !string.IsNullOrWhiteSpace(cliente.Nombre) || !string.IsNullOrWhiteSpace(cliente.Direccion));

		private static ComprobanteVentaPagoDto? NormalizarPago(ComprobanteVentaPagoDto? pago)
		{
			if (pago is null)
			{
				return null;
			}

			pago.FormaPago = (pago.FormaPago ?? string.Empty).Trim().ToUpperInvariant();
			pago.Cuotas ??= new List<ComprobanteVentaCuotaDto>();

			foreach (var cuota in pago.Cuotas)
			{
				cuota.FechaVencimiento = (cuota.FechaVencimiento ?? string.Empty).Trim();
			}

			return pago;
		}

		private static void ValidarSolicitud(
			string tipo,
			VentaComprobanteDisponibleDto venta,
			ComprobanteVentaClienteDto clienteFiscal,
			DateTime fechaEmision,
			DateTime? fechaVencimiento,
			string moneda,
			ComprobanteVentaPagoDto? pago,
			string? receptorSource)
		{
			if (moneda is not ("PEN" or "USD"))
			{
				throw new InvalidOperationException("La moneda permitida es PEN o USD.");
			}

			if (venta.Detalle.Count == 0 || venta.Detalle.Any(x =>
				x.Cantidad <= 0 ||
				x.Precio < 0 ||
				string.IsNullOrWhiteSpace(x.ProductoServicio)))
			{
				throw new InvalidOperationException("La venta debe contener al menos un ítem válido.");
			}

			if (tipo == "BOLETA")
			{
				ValidarBoleta(clienteFiscal, receptorSource);
				return;
			}

			ValidarFactura(clienteFiscal, fechaEmision, fechaVencimiento, venta.Total, pago);
		}

		private static void ValidarBoleta(ComprobanteVentaClienteDto cliente, string? receptorSource)
		{
			if (string.Equals((receptorSource ?? string.Empty).Trim(), "UNIDENTIFIED", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var documento = (cliente.Documento ?? string.Empty).Trim();
			var tipoDocumento = (cliente.TipoDocumento ?? string.Empty).Trim().ToUpperInvariant();

			if (string.IsNullOrWhiteSpace(documento))
			{
				return;
			}

			if (tipoDocumento is not ("DNI" or "RUC" or "CE"))
			{
				throw new InvalidOperationException("En boleta solo se permite DNI, RUC o CE.");
			}

			if (!DocumentoValido(tipoDocumento, documento))
			{
				throw new InvalidOperationException("El documento del cliente no cumple el formato esperado.");
			}

			if (string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Direccion))
			{
				throw new InvalidOperationException("Si la boleta tiene documento, el nombre y la dirección son obligatorios.");
			}
		}

		private static void ValidarFactura(
			ComprobanteVentaClienteDto cliente,
			DateTime fechaEmision,
			DateTime? fechaVencimiento,
			decimal total,
			ComprobanteVentaPagoDto? pago)
		{
			if (string.IsNullOrWhiteSpace(cliente.Nombre))
			{
				throw new InvalidOperationException("La razón social del cliente es obligatoria para factura.");
			}

			if (!string.Equals((cliente.TipoDocumento ?? string.Empty).Trim(), "RUC", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("La factura solo permite RUC como tipo de documento.");
			}

			if (!DocumentoValido("RUC", cliente.Documento))
			{
				throw new InvalidOperationException("El RUC del cliente debe tener 11 dígitos.");
			}

			if (fechaVencimiento.HasValue && fechaVencimiento.Value.Date <= fechaEmision.Date)
			{
				throw new InvalidOperationException("La fecha de vencimiento debe ser mayor a la fecha de emisión.");
			}

			if (pago is null || string.IsNullOrWhiteSpace(pago.FormaPago))
			{
				throw new InvalidOperationException("La factura requiere forma de pago.");
			}

			if (pago.FormaPago is not ("CONTADO" or "CREDITO"))
			{
				throw new InvalidOperationException("La forma de pago permitida es CONTADO o CREDITO.");
			}

			if (pago.FormaPago == "CONTADO")
			{
				if (pago.Cuotas.Count > 0)
				{
					throw new InvalidOperationException("La factura al contado no debe registrar cuotas.");
				}

				return;
			}

			if (pago.Cuotas.Count == 0)
			{
				throw new InvalidOperationException("La factura a crédito debe registrar al menos una cuota.");
			}

			var sumaCuotas = 0m;
			foreach (var cuota in pago.Cuotas)
			{
				if (cuota.Monto <= 0 || cuota.Monto > total)
				{
					throw new InvalidOperationException("Cada cuota debe ser mayor a 0.01 y no superar el total del comprobante.");
				}

				var fechaCuota = ParsearFechaObligatoria(cuota.FechaVencimiento, "La fecha de vencimiento de la cuota no es válida.");
				if (fechaCuota.Date <= DateTime.Today)
				{
					throw new InvalidOperationException("Cada cuota debe vencer después del día actual.");
				}

				sumaCuotas += cuota.Monto;
			}

			if (Redondear(sumaCuotas) != Redondear(total))
			{
				throw new InvalidOperationException("La suma de cuotas debe coincidir exactamente con el importe total de la factura.");
			}
		}

		private static UblInvoicePayloadDto CrearBoletaRequest(
			string serie,
			string numero,
			DateTime fechaEmision,
			string horaEmision,
			string moneda,
			VentaComprobanteDisponibleDto venta,
			ComprobanteVentaClienteDto clienteFiscal)
		{
			return new UblInvoicePayloadDto
			{
				Serie = serie,
				Correlativo = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				HoraEmision = horaEmision,
				Moneda = moneda,
				MontoEnLetras = MontoEnLetras.EnSoles(venta.Total),
				Cliente = new UblPartyPayloadDto
				{
					TipoDocumento = MapearTipoDocumentoSunat(clienteFiscal.TipoDocumento, false),
					NumeroDocumento = string.IsNullOrWhiteSpace(clienteFiscal.Documento) ? "-" : clienteFiscal.Documento,
					Nombre = string.IsNullOrWhiteSpace(clienteFiscal.Nombre) ? "CLIENTES VARIOS" : clienteFiscal.Nombre,
					Direccion = string.IsNullOrWhiteSpace(clienteFiscal.Direccion) ? null : clienteFiscal.Direccion
				},
				Totales = new UblTotalsPayloadDto
				{
					ValorVenta = venta.Subtotal,
					Igv = venta.Igv,
					Total = venta.Total
				},
				Items = venta.Detalle.Select(x => new UblItemPayloadDto
				{
					Descripcion = x.ProductoServicio,
					Cantidad = x.Cantidad,
					PrecioUnitario = x.Precio,
					ValorVenta = Redondear(x.Precio * x.Cantidad),
					Igv = x.Igv,
					PrecioConIgv = Redondear(x.Importe / (x.Cantidad <= 0 ? 1 : x.Cantidad)),
					UnidadMedida = x.UnidadMedida,
					PorcentajeIgv = 18,
					CodigoAfectacionIgv = "10"
				}).ToList()
			};
		}

		private static UblInvoicePayloadDto CrearFacturaRequest(
			string serie,
			string numero,
			DateTime fechaEmision,
			DateTime? fechaVencimiento,
			string horaEmision,
			string moneda,
			VentaComprobanteDisponibleDto venta,
			ComprobanteVentaClienteDto clienteFiscal,
			ComprobanteVentaPagoDto? pago)
		{
			return new UblInvoicePayloadDto
			{
				Serie = serie,
				Correlativo = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				FechaVencimiento = fechaVencimiento?.ToString("yyyy-MM-dd"),
				HoraEmision = horaEmision,
				Moneda = moneda,
				MontoEnLetras = MontoEnLetras.EnSoles(venta.Total),
				Cliente = new UblPartyPayloadDto
				{
					TipoDocumento = MapearTipoDocumentoSunat(clienteFiscal.TipoDocumento, true),
					NumeroDocumento = clienteFiscal.Documento,
					Nombre = clienteFiscal.Nombre,
					Direccion = string.IsNullOrWhiteSpace(clienteFiscal.Direccion) ? null : clienteFiscal.Direccion
				},
				Totales = new UblTotalsPayloadDto
				{
					ValorVenta = venta.Subtotal,
					Igv = venta.Igv,
					Total = venta.Total
				},
				Items = venta.Detalle.Select(x => new UblItemPayloadDto
				{
					Descripcion = x.ProductoServicio,
					Cantidad = x.Cantidad,
					PrecioUnitario = x.Precio,
					ValorVenta = Redondear(x.Precio * x.Cantidad),
					Igv = x.Igv,
					PrecioConIgv = Redondear(x.Importe / (x.Cantidad <= 0 ? 1 : x.Cantidad)),
					UnidadMedida = x.UnidadMedida,
					PorcentajeIgv = 18,
					CodigoAfectacionIgv = "10"
				}).ToList(),
				Pago = pago is null
					? null
					: new UblPaymentPayloadDto
					{
						FormaPago = pago.FormaPago == "CREDITO" ? "Credito" : "Contado",
						Cuotas = pago.Cuotas.Select(x => new UblInstallmentPayloadDto
						{
							Monto = x.Monto,
							FechaVencimiento = x.FechaVencimiento
						}).ToList()
					}
			};
		}

		private static bool DocumentoValido(string tipoDocumento, string? numero) =>
			FacturacionVoucherHelper.DocumentoValido(tipoDocumento, numero);

		private static DateTime ParsearFechaObligatoria(string? fechaTexto, string mensaje) =>
			FacturacionVoucherHelper.ParsearFechaObligatoria(fechaTexto, mensaje);

		private static decimal Redondear(decimal valor) =>
			Math.Round(valor, 2, MidpointRounding.AwayFromZero);

		private static decimal ObtenerIgvVenta(SqlDataReader dr)
		{
			var igv = dr["igv"];
			if (igv == DBNull.Value)
			{
				return 18m;
			}

			var porcentaje = Convert.ToDecimal(igv);
			return porcentaje <= 0 ? 18m : porcentaje;
		}

		private static decimal ObtenerPorcentajeIgv(VentaComprobanteDetalleDto item)
		{
			var baseAmount = Redondear(item.Cantidad * item.Precio);
			if (baseAmount <= 0)
			{
				return 18m;
			}

			return Redondear((item.Igv / baseAmount) * 100m);
		}

		private static string MapearTipoDocumentoSunat(string tipoDocumento, bool esFactura)
		{
			if (esFactura)
			{
				return "6";
			}

			return (tipoDocumento ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"DNI" => "1",
				"RUC" => "6",
				"CE" => "4",
				_ => "-"
			};
		}

		private static string MapearTipoDocumentoUi(string? tipoDocumento)
		{
			return (tipoDocumento ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"RUC" => "RUC",
				"DNI" => "DNI",
				"CE" => "CE",
				"PASAPORTE" => "PASAPORTE",
				"6" => "RUC",
				"1" => "DNI",
				"4" => "CE",
				_ => string.Empty
			};
		}

		private static string MapearEstadoUi(string? operationType, string? transmissionStatus, string? sunatStatus)
		{
			if (string.Equals(operationType, "VOID", StringComparison.OrdinalIgnoreCase) &&
				string.Equals(transmissionStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase))
			{
				return "ANULADO";
			}

			return (sunatStatus ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"RECHAZADO" => "RECHAZADO",
				_ => "EMITIDO"
			};
		}

		private static string MapearEstadoSunatUi(string? estado)
		{
			return (estado ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"ANULADO" => "ANULADO",
				"ACEPTADO" => "ACEPTADO",
				"RECHAZADO" => "RECHAZADO",
				"EXCEPCION" => "EXCEPCION",
				"OBSERVADO" => "OBSERVADO",
				"ENVIADO" => "ENVIADO",
				"NO_ENVIADO" => "PENDIENTE",
				_ => "PENDIENTE"
			};
		}

		private static bool EsAnulacionConfirmada(string? estado, string? estadoSunat) =>
			string.Equals((estado ?? string.Empty).Trim(), "ANULADO", StringComparison.OrdinalIgnoreCase) &&
			string.Equals((estadoSunat ?? string.Empty).Trim(), "ANULADO", StringComparison.OrdinalIgnoreCase);

		private static string MapearEstadoSunatPersistencia(FacturacionEnvioResultado envio)
		{
			var estado = string.IsNullOrWhiteSpace(envio.EstadoSunat)
				? (envio.Exitoso ? "PENDIENTE" : "RECHAZADO")
				: envio.EstadoSunat.Trim().ToUpperInvariant();

			return estado switch
			{
				"NO_ENVIADO" => "NO_ENVIADO",
				"PENDIENTE" => "PENDIENTE",
				"ACEPTADO" => "ACEPTADO",
				"RECHAZADO" => "RECHAZADO",
				"EXCEPCION" => "EXCEPCION",
				"OBSERVADO" => "EXCEPCION",
				"ENVIADO" => "PENDIENTE",
				"PENDING" => "PENDIENTE",
				"SUCCESS" => "PENDIENTE",
				"ERROR" => "EXCEPCION",
				"FAILED" => "RECHAZADO",
				_ => envio.Exitoso ? "PENDIENTE" : "EXCEPCION"
			};
		}

		private static string NormalizarSunatStatusParaVoucher(FacturacionEnvioResultado envio)
		{
			var estado = MapearEstadoSunatPersistencia(envio)
				.Replace(" ", string.Empty, StringComparison.Ordinal)
				.Replace("-", string.Empty, StringComparison.Ordinal)
				.Replace("_", string.Empty, StringComparison.Ordinal)
				.Trim()
				.ToUpperInvariant();

			return estado switch
			{
				"NOENVIADO" => "NO_ENVIADO",
				"PENDIENTE" => "PENDIENTE",
				"ACEPTADO" => "ACEPTADO",
				"RECHAZADO" => "RECHAZADO",
				"EXCEPCION" => "EXCEPCION",
				_ => envio.Exitoso ? "PENDIENTE" : "EXCEPCION"
			};
		}

		private static bool EsRetryable(FacturacionEnvioResultado resultado) =>
			resultado.StatusCode == StatusCodes.Status502BadGateway ||
			resultado.StatusCode == StatusCodes.Status503ServiceUnavailable ||
			resultado.StatusCode == StatusCodes.Status504GatewayTimeout;

		private static List<string> SepararObservaciones(string? observaciones)
		{
			if (string.IsNullOrWhiteSpace(observaciones))
			{
				return new List<string>();
			}

			return observaciones
				.Split(['#', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Take(20)
				.ToList();
		}

		private static (string? A4, string? A5, string? Ticket58, string? Ticket80) ExtraerUrlsPdf(object? respuestaApi)
		{
			if (respuestaApi is not System.Text.Json.JsonElement element || element.ValueKind != System.Text.Json.JsonValueKind.Object)
			{
				return (null, null, null, null);
			}

			if (!element.TryGetProperty("pdf", out var pdf) || pdf.ValueKind != System.Text.Json.JsonValueKind.Object)
			{
				return (null, null, null, null);
			}

			return (
				pdf.TryGetProperty("A4", out var a4) ? a4.GetString() : null,
				pdf.TryGetProperty("A5", out var a5) ? a5.GetString() : null,
				pdf.TryGetProperty("58mm", out var p58) ? p58.GetString() : null,
				pdf.TryGetProperty("80mm", out var p80) ? p80.GetString() : null
			);
		}
	}
}
