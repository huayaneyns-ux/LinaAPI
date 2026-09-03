using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Models.Facturacion.Documentos;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class DocumentoFacturacionService
	{
		private readonly Conexion _conexion;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionSettings _settings;

		public DocumentoFacturacionService(
			Conexion conexion,
			FacturacionSunatService facturacionSunatService,
			IOptions<FacturacionSettings> options)
		{
			_conexion = conexion;
			_facturacionSunatService = facturacionSunatService;
			_settings = options.Value;
		}

		public async Task<List<ComprobanteVentaListItemDto>> ListarTodosAsync()
		{
			var vouchers = new Dictionary<string, ComprobanteVentaListItemDto>(StringComparer.OrdinalIgnoreCase);

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					v.Id,
					v.VentaId,
					v.CompraId,
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
					COALESCE(cust.DocumentType, '') AS CustomerDocumentType,
					COALESCE(cust.DocumentNumber, '') AS CustomerDocumentNumber,
					COALESCE(cust.Name, '') AS CustomerName,
					COALESCE(cust.Address, '') AS CustomerAddress,
					COALESCE(seller.DocumentType, '') AS SellerDocumentType,
					COALESCE(seller.DocumentNumber, '') AS SellerDocumentNumber,
					COALESCE(seller.Name, '') AS SellerName,
					COALESCE(seller.Address, '') AS SellerAddress,
					COALESCE(ref.Id, '00000000-0000-0000-0000-000000000000') AS ReferencedVoucherId,
					COALESCE(ref.Series, '') AS ReferencedSeries,
					COALESCE(ref.Number, '') AS ReferencedNumber,
					COALESCE(adj.ReasonCode, '') AS AdjustmentReasonCode,
					COALESCE(adj.ReasonDescription, '') AS AdjustmentReasonDescription,
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
				LEFT JOIN dbo.VoucherParty cust
					ON cust.VoucherId = v.Id
				   AND cust.Role = 'CUSTOMER'
				LEFT JOIN dbo.VoucherParty seller
					ON seller.VoucherId = v.Id
				   AND seller.Role = 'SELLER'
				LEFT JOIN dbo.VoucherAdjustment adj
					ON adj.VoucherId = v.Id
				LEFT JOIN dbo.Voucher ref
					ON ref.Id = adj.ReferencedVoucherId
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
				WHERE v.SunatTypeCode IN ('01', '03', '04', '07', '08')
				ORDER BY v.CreatedAt DESC, vi.LineNumber ASC, inst.InstallmentNumber ASC, vo.LineNumber ASC;
				""";

			using var cmd = new SqlCommand(sql, con) { CommandType = CommandType.Text };
			using var dr = await cmd.ExecuteReaderAsync();

			while (await dr.ReadAsync())
			{
				var id = dr["Id"]?.ToString() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				if (!vouchers.TryGetValue(id, out var voucher))
				{
					var paymentCondition = dr["PaymentCondition"]?.ToString() ?? string.Empty;
					var clienteNombre = PreferValue(dr["CustomerName"], dr["SellerName"]);
					var clienteDocumento = PreferValue(dr["CustomerDocumentNumber"], dr["SellerDocumentNumber"]);
					var clienteTipoDocumento = PreferValue(dr["CustomerDocumentType"], dr["SellerDocumentType"]);
					var clienteDireccion = PreferValue(dr["CustomerAddress"], dr["SellerAddress"]);
					var sunatTypeCode = dr["SunatTypeCode"]?.ToString();

					voucher = new ComprobanteVentaListItemDto
					{
						Id = id,
						Tipo = MapearTipo(sunatTypeCode),
						Serie = dr["Series"]?.ToString() ?? string.Empty,
						Numero = dr["Number"]?.ToString() ?? string.Empty,
						FechaEmision = Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
						Cliente = clienteNombre,
						DocumentoCliente = clienteDocumento,
						Subtotal = dr["Subtotal"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Subtotal"]),
						Igv = dr["Igv"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Igv"]),
						Total = dr["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Total"]),
						Estado = MapearEstadoUi(
							dr["LastOperationType"]?.ToString(),
							dr["LastTransmissionStatus"]?.ToString(),
							dr["SunatStatus"]?.ToString(),
							dr["SunatDocumentId"]?.ToString()),
						EstadoSunat = FacturacionVoucherHelper.MapearEstadoSunatUi(dr["SunatStatus"]?.ToString()),
						TipoDocumentoCliente = clienteTipoDocumento,
						DireccionCliente = clienteDireccion,
						CorreoCliente = string.Empty,
						CodigoRespuestaSunat = dr["LastHttpStatus"] == DBNull.Value || Convert.ToInt32(dr["LastHttpStatus"]) == 0
							? string.Empty
							: Convert.ToInt32(dr["LastHttpStatus"]).ToString(CultureInfo.InvariantCulture),
						MensajeSunat = dr["LastErrorMessage"]?.ToString() ?? string.Empty,
						FechaConsultaSunat = dr["LastRespondedAt"] == DBNull.Value
							? Convert.ToDateTime(dr["UpdatedAt"]).ToString("s")
							: Convert.ToDateTime(dr["LastRespondedAt"]).ToString("s"),
						FechaEnvioSunat = Convert.ToDateTime(dr["UpdatedAt"]).ToString("s"),
						DocumentId = dr["SunatDocumentId"] == DBNull.Value ? null : dr["SunatDocumentId"].ToString(),
						FileName = $"{_settings.Emisor.Ruc}-{sunatTypeCode}-{dr["Series"]}-{dr["Number"]}",
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
					var importe = dr["ItemTotal"] == DBNull.Value
						? FacturacionVoucherHelper.Redondear((cantidad * precio) + igv)
						: Convert.ToDecimal(dr["ItemTotal"]);

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
					!voucher.Pago.Cuotas.Any(x => x.Numero == Convert.ToInt32(dr["InstallmentNumber"])))
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

		public async Task<ComprobanteVentaListItemDto> ObtenerDetalleAsync(string id)
		{
			if (!Guid.TryParse(id, out var voucherId))
			{
				throw new InvalidOperationException("El identificador del voucher no es válido.");
			}

			return (await ListarTodosAsync()).FirstOrDefault(x => string.Equals(x.Id, voucherId.ToString(), StringComparison.OrdinalIgnoreCase))
				?? throw new InvalidOperationException("No se encontró el documento seleccionado.");
		}

		public async Task<DocumentoFacturacionDto> ObtenerPorIdAsync(string id)
		{
			if (!Guid.TryParse(id, out var voucherId))
			{
				throw new InvalidOperationException("El identificador del voucher no es válido.");
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					v.Id,
					v.VentaId,
					v.CompraId,
					v.SunatTypeCode,
					v.Series,
					v.Number,
					v.IssueDate,
					v.Currency,
					v.Subtotal,
					v.Igv,
					v.Total,
					v.SunatStatus,
					v.SunatDocumentId,
					ref.Id AS ReferencedVoucherId,
					ref.Series AS ReferencedSeries,
					ref.Number AS ReferencedNumber,
					COALESCE(st.OperationType, '') AS LastOperationType,
					COALESCE(st.TransmissionStatus, '') AS LastTransmissionStatus,
					COALESCE(st.HttpStatus, 0) AS LastHttpStatus,
					COALESCE(st.ErrorMessage, '') AS LastErrorMessage
				FROM dbo.Voucher v
				LEFT JOIN dbo.VoucherAdjustment va
					ON va.VoucherId = v.Id
				LEFT JOIN dbo.Voucher ref
					ON ref.Id = va.ReferencedVoucherId
				OUTER APPLY (
					SELECT TOP 1
						t.OperationType,
						t.TransmissionStatus,
						t.HttpStatus,
						t.ErrorMessage
					FROM dbo.SunatTransmission t
					WHERE t.VoucherId = v.Id
					ORDER BY t.CreatedAt DESC, t.AttemptNumber DESC
				) st
				WHERE v.Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);

			using var dr = await cmd.ExecuteReaderAsync();
			if (!await dr.ReadAsync())
			{
				throw new InvalidOperationException("No se encontró el documento seleccionado.");
			}

			return new DocumentoFacturacionDto
			{
				Id = dr["Id"]?.ToString() ?? string.Empty,
				Tipo = MapearTipo(dr["SunatTypeCode"]?.ToString()),
				Serie = dr["Series"]?.ToString() ?? string.Empty,
				Numero = dr["Number"]?.ToString() ?? string.Empty,
				FechaEmision = Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
				Moneda = dr["Currency"]?.ToString() ?? "PEN",
				Estado = MapearEstadoUi(
					dr["LastOperationType"]?.ToString(),
					dr["LastTransmissionStatus"]?.ToString(),
					dr["SunatStatus"]?.ToString(),
					dr["SunatDocumentId"]?.ToString()),
				EstadoSunat = FacturacionVoucherHelper.MapearEstadoSunatUi(dr["SunatStatus"]?.ToString()),
				DocumentId = dr["SunatDocumentId"] == DBNull.Value ? null : dr["SunatDocumentId"].ToString(),
				CodigoRespuestaSunat = dr["LastHttpStatus"] == DBNull.Value || Convert.ToInt32(dr["LastHttpStatus"]) == 0
					? string.Empty
					: Convert.ToInt32(dr["LastHttpStatus"]).ToString(CultureInfo.InvariantCulture),
				MensajeSunat = dr["LastErrorMessage"]?.ToString() ?? string.Empty,
				DetalleError = dr["LastErrorMessage"]?.ToString() ?? string.Empty,
				Subtotal = dr["Subtotal"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Subtotal"]),
				Igv = dr["Igv"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Igv"]),
				Total = dr["Total"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Total"]),
				VentaOrigenId = dr["VentaId"] == DBNull.Value ? null : dr["VentaId"].ToString(),
				CompraOrigenId = dr["CompraId"] == DBNull.Value ? null : dr["CompraId"].ToString(),
				VoucherReferenciaId = dr["ReferencedVoucherId"] == DBNull.Value ? null : dr["ReferencedVoucherId"].ToString(),
				DocumentoReferencia = ResolverDocumentoReferencia(dr)
			};
		}

		public async Task<DocumentoFacturacionDto> SincronizarEstadoSunatAsync(string id)
		{
			var voucher = await ObtenerPorIdAsync(id);
			if (string.IsNullOrWhiteSpace(voucher.DocumentId))
			{
				throw new InvalidOperationException("El documento no tiene documentId registrado en APISUNAT.");
			}

			var consulta = await _facturacionSunatService.ObtenerDocumentoPorId(voucher.DocumentId);
			if (!consulta.Exitoso)
			{
				throw new InvalidOperationException(consulta.DetalleError ?? consulta.MensajeSunat ?? consulta.Mensaje);
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();
			await FacturacionVoucherHelper.ActualizarVoucherPostConsultaAsync(con, Guid.Parse(voucher.Id), consulta);
			await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, Guid.Parse(voucher.Id), "STATUS_QUERY", consulta);

			return await ObtenerPorIdAsync(id);
		}

		public async Task<(byte[] Content, string ContentType, string FileName)> DescargarPdfAsync(string id, string format)
		{
			var voucher = await ObtenerDetalleAsync(id);
			if (string.IsNullOrWhiteSpace(voucher.DocumentId))
			{
				throw new InvalidOperationException("El documento no tiene documentId registrado en APISUNAT.");
			}

			var formato = (format ?? string.Empty).Trim();
			if (formato is not ("A4" or "A5" or "ticket58mm" or "ticket80mm"))
			{
				throw new InvalidOperationException("El formato PDF solicitado no es válido.");
			}

			var tipoCode = voucher.Tipo switch
			{
				"FACTURA" => "01",
				"BOLETA" => "03",
				"LIQUIDACION_COMPRA" => "04",
				"NOTA_CREDITO" => "07",
				"NOTA_DEBITO" => "08",
				_ => "00"
			};

			var fileName = string.IsNullOrWhiteSpace(voucher.FileName)
				? $"{_settings.Emisor.Ruc}-{tipoCode}-{voucher.Serie}-{voucher.Numero}"
				: voucher.FileName;

			return await _facturacionSunatService.DescargarPdf(voucher.DocumentId, formato, fileName);
		}

		public async Task<DocumentoFacturacionDto> AnularAsync(string id, string reason)
		{
			var voucher = await ObtenerPorIdAsync(id);
			if (string.IsNullOrWhiteSpace(voucher.DocumentId))
			{
				throw new InvalidOperationException("El documento no tiene documentId registrado en APISUNAT.");
			}

			var motivo = (reason ?? string.Empty).Trim();
			if (motivo.Length < 3 || motivo.Length > 100)
			{
				throw new InvalidOperationException("El motivo de anulación debe tener entre 3 y 100 caracteres.");
			}

			var resultado = await _facturacionSunatService.AnularDocumento(voucher.DocumentId, motivo);
			if (!resultado.Exitoso)
			{
				throw new InvalidOperationException(resultado.DetalleError ?? resultado.MensajeSunat ?? resultado.Mensaje);
			}

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();
			await FacturacionVoucherHelper.ActualizarVoucherPostAnulacionAsync(con, Guid.Parse(voucher.Id), resultado);
			await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, Guid.Parse(voucher.Id), "VOID", resultado);

			return await ObtenerPorIdAsync(id);
		}

		private static string ResolverDocumentoReferencia(SqlDataReader dr)
		{
			if (dr["ReferencedSeries"] != DBNull.Value && dr["ReferencedNumber"] != DBNull.Value)
			{
				return $"{dr["ReferencedSeries"]}-{dr["ReferencedNumber"]}";
			}

			if (dr["VentaId"] != DBNull.Value)
			{
				return $"VTA-{Convert.ToInt32(dr["VentaId"]):D6}";
			}

			if (dr["CompraId"] != DBNull.Value)
			{
				return $"COM-{Convert.ToInt32(dr["CompraId"]):D6}";
			}

			return string.Empty;
		}

		private static string PreferValue(object? preferred, object? fallback)
		{
			var preferredValue = preferred == DBNull.Value ? string.Empty : preferred?.ToString() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(preferredValue))
			{
				return preferredValue;
			}

			return fallback == DBNull.Value ? string.Empty : fallback?.ToString() ?? string.Empty;
		}

		private static string MapearTipo(string? sunatTypeCode) =>
			(sunatTypeCode ?? string.Empty).Trim() switch
			{
				"01" => "FACTURA",
				"03" => "BOLETA",
				"04" => "LIQUIDACION_COMPRA",
				"07" => "NOTA_CREDITO",
				"08" => "NOTA_DEBITO",
				_ => "COMPROBANTE"
			};

		private static string MapearEstadoUi(string? operationType, string? transmissionStatus, string? sunatStatus, string? documentId = null)
		{
			var transmission = (transmissionStatus ?? string.Empty).Trim().ToUpperInvariant();
			if (transmission == "ERROR")
			{
				return "RECHAZADO";
			}

			var operation = (operationType ?? string.Empty).Trim().ToUpperInvariant();
			var normalizedSunatStatus = (sunatStatus ?? string.Empty).Trim().ToUpperInvariant();

			if (string.IsNullOrWhiteSpace(operation) &&
				string.IsNullOrWhiteSpace(normalizedSunatStatus) &&
				string.IsNullOrWhiteSpace(documentId))
			{
				return "BORRADOR";
			}

			return operation switch
			{
				"VOID" when normalizedSunatStatus == "RECHAZADO" => "RECHAZADO",
				"VOID" => "ANULADO",
				"STATUS_QUERY" when normalizedSunatStatus == "RECHAZADO" => "RECHAZADO",
				_ when normalizedSunatStatus == "RECHAZADO" => "RECHAZADO",
				_ => "EMITIDO"
			};
		}
	}
}
