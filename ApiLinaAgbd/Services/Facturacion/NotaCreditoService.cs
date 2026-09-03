using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.NotaCredito;
using ApiLinaAgbd.Models.Facturacion.Notas;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using ApiLinaAgbd.Services;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class NotaCreditoService
	{
		private const string CodigoAfectacionIgvGravado = "10";

		private const string SerieFactura = "FC01";
		private const string SerieBoleta = "BC01";
		private readonly Conexion _conexion;
		private readonly NotaCreditoUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionPdfLocalService _pdfLocalService;
		private readonly FacturacionSettings _settings;

		public NotaCreditoService(
			Conexion conexion,
			NotaCreditoUblBuilder builder,
			FacturacionSunatService facturacionSunatService,
			FacturacionPdfLocalService pdfLocalService,
			IOptions<FacturacionSettings> options)
		{
			_conexion = conexion;
			_builder = builder;
			_facturacionSunatService = facturacionSunatService;
			_pdfLocalService = pdfLocalService;
			_settings = options.Value;
		}

		public async Task<List<NotaComprobanteBaseDisponibleDto>> ListarComprobantesBaseAsync()
		{
			var vouchers = new Dictionary<string, NotaComprobanteBaseDisponibleDto>(StringComparer.OrdinalIgnoreCase);

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					v.Id,
					v.SunatTypeCode,
					v.Series,
					v.Number,
					v.IssueDate,
					v.Currency,
					v.Subtotal,
					v.Igv,
					v.Total,
					COALESCE(vp.Name, '') AS ClienteNombre,
					COALESCE(vp.DocumentType, '') AS ClienteTipoDocumento,
					COALESCE(vp.DocumentNumber, '') AS ClienteDocumento,
					COALESCE(vp.Address, '') AS ClienteDireccion,
					COALESCE(vi.Id, '00000000-0000-0000-0000-000000000000') AS ItemId,
					COALESCE(vi.ProductId, 0) AS ProductId,
					COALESCE(vi.ProductCode, '') AS ProductCode,
					COALESCE(vi.Description, '') AS Description,
					COALESCE(vi.Quantity, 0) AS Quantity,
					COALESCE(vi.UnitPrice, 0) AS UnitPrice,
					COALESCE(vi.SaleValue, 0) AS SaleValue,
					COALESCE(vi.Igv, 0) AS ItemIgv,
					COALESCE(vi.Total, 0) AS ItemTotal,
					COALESCE(vi.UnitCode, 'NIU') AS UnitCode
				FROM dbo.Voucher v
				LEFT JOIN dbo.VoucherParty vp
					ON vp.VoucherId = v.Id
				   AND vp.Role = 'CUSTOMER'
				LEFT JOIN dbo.VoucherItem vi
					ON vi.VoucherId = v.Id
				WHERE v.SunatTypeCode IN ('01', '03')
				ORDER BY v.CreatedAt DESC, vi.LineNumber ASC;
				""";

			using var cmd = new SqlCommand(sql, con);
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
					var sunatType = dr["SunatTypeCode"]?.ToString() ?? string.Empty;
					voucher = new NotaComprobanteBaseDisponibleDto
					{
						Id = id,
						Tipo = sunatType == "01" ? "FACTURA" : "BOLETA",
						SunatTypeCode = sunatType,
						Serie = dr["Series"]?.ToString() ?? string.Empty,
						Numero = dr["Number"]?.ToString() ?? string.Empty,
						FechaEmision = Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
						Moneda = dr["Currency"]?.ToString() ?? "PEN",
						ClienteNombre = dr["ClienteNombre"]?.ToString() ?? string.Empty,
						ClienteTipoDocumento = dr["ClienteTipoDocumento"]?.ToString() ?? string.Empty,
						ClienteDocumento = dr["ClienteDocumento"]?.ToString() ?? string.Empty,
						ClienteDireccion = dr["ClienteDireccion"]?.ToString() ?? string.Empty,
						Subtotal = dr["Subtotal"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Subtotal"]),
						Igv = dr["Igv"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Igv"]),
						Total = dr["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Total"])
					};

					vouchers.Add(id, voucher);
				}

				var itemId = dr["ItemId"]?.ToString();
				if (string.IsNullOrWhiteSpace(itemId) || itemId == "00000000-0000-0000-0000-000000000000")
				{
					continue;
				}

				var cantidad = Convert.ToDecimal(dr["Quantity"]);
				var precioUnitario = dr["UnitPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["UnitPrice"]);
				var valorVenta = dr["SaleValue"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["SaleValue"]);
				var itemIgv = dr["ItemIgv"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["ItemIgv"]);
				var itemTotal = dr["ItemTotal"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["ItemTotal"]);

				voucher.Items.Add(new NotaComprobanteBaseItemDto
				{
					Id = itemId,
					ProductoId = Convert.ToInt32(dr["ProductId"]) <= 0 ? null : Convert.ToInt32(dr["ProductId"]),
					Codigo = dr["ProductCode"]?.ToString() ?? string.Empty,
					Descripcion = dr["Description"]?.ToString() ?? string.Empty,
					Cantidad = cantidad,
					PrecioUnitario = ResolverPrecioUnitarioBase(cantidad, precioUnitario, valorVenta, itemTotal),
					ValorVenta = valorVenta,
					Igv = itemIgv,
					Importe = itemTotal,
					UnidadMedida = dr["UnitCode"]?.ToString() ?? "NIU"
				});
			}

			return vouchers.Values.ToList();
		}

		private static decimal ResolverPrecioUnitarioBase(decimal cantidad, decimal precioUnitario, decimal valorVenta, decimal importe)
		{
			if (precioUnitario > 0)
			{
				return precioUnitario;
			}

			if (cantidad > 0 && valorVenta > 0)
			{
				return FacturacionVoucherHelper.Redondear(valorVenta / cantidad);
			}

			if (cantidad > 0 && importe > 0)
			{
				return FacturacionVoucherHelper.Redondear(importe / cantidad);
			}

			return 0m;
		}

		public async Task<NotaComprobanteResultadoDto> EmitirAsync(NotaCreditoEmitirRequestDto request)
		{
			ValidarConfiguracion();
			var referencia = (await ListarComprobantesBaseAsync()).FirstOrDefault(x => x.Id == request.VoucherReferenciaId)
				?? throw new InvalidOperationException("El comprobante base no existe.");

			var items = PrepararItems(request, referencia);
			var itemsCalculados = CalcularItems(items, request.IgvPorcentaje);
			ValidarSolicitud(request, referencia, itemsCalculados);
			var subtotal = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.ValorVenta));
			var igv = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.Igv));
			var total = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.Importe));
			var fechaEmision = FacturacionVoucherHelper.ParsearFechaObligatoria(request.FechaEmision, "La fecha de emisión no es válida.");
			var serie = referencia.SunatTypeCode == "01" ? SerieFactura : SerieBoleta;
			var voucherId = Guid.NewGuid();
			string numero;

			using (var con = _conexion.ObtenerConexion())
			{
				await con.OpenAsync();
				using var tx = con.BeginTransaction();

				numero = await FacturacionVoucherHelper.GenerarNumeroAleatorioDisponibleAsync(con, tx, "07", serie, _settings.Emisor.Ruc);
				await InsertarVoucherAsync(con, tx, voucherId, referencia, fechaEmision, serie, numero, request.Moneda, subtotal, igv, total);
				await FacturacionVoucherHelper.InsertarPartyAsync(con, tx, voucherId, "CUSTOMER", referencia.ClienteTipoDocumento, referencia.ClienteDocumento, referencia.ClienteNombre, referencia.ClienteDireccion);
				await InsertarItemsAsync(con, tx, voucherId, itemsCalculados, referencia);
				await FacturacionVoucherHelper.InsertarAdjustmentAsync(con, tx, voucherId, Guid.Parse(referencia.Id), request.Motivo.Codigo, request.Motivo.Descripcion);
				await FacturacionVoucherHelper.InsertarObservacionesAsync(con, tx, voucherId, request.Observaciones);
				tx.Commit();
			}

			var body = _builder.Build(new UblAdjustmentPayloadDto
			{
				Serie = serie,
				Correlativo = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				HoraEmision = request.HoraEmision,
				Moneda = request.Moneda,
				DocumentoReferencia = new UblReferenceDocumentPayloadDto
				{
					Id = $"{referencia.Serie}-{referencia.Numero}",
					TipoDocumento = referencia.SunatTypeCode
				},
				Motivo = new UblReasonPayloadDto
				{
					Codigo = request.Motivo.Codigo,
					Descripcion = request.Motivo.Descripcion
				},
				Cliente = new UblPartyPayloadDto
				{
					TipoDocumento = FacturacionVoucherHelper.MapearTipoDocumentoSunat(referencia.ClienteTipoDocumento, referencia.SunatTypeCode == "01"),
					NumeroDocumento = string.IsNullOrWhiteSpace(referencia.ClienteDocumento) ? "-" : referencia.ClienteDocumento,
					Nombre = string.IsNullOrWhiteSpace(referencia.ClienteNombre) ? "CLIENTES VARIOS" : referencia.ClienteNombre,
					Direccion = referencia.ClienteDireccion
				},
				Totales = new UblTotalsPayloadDto
				{
					ValorVenta = subtotal,
					Igv = igv,
					Total = total
				},
				Items = itemsCalculados.Select(MapearItemUbl).ToList()
			}, referencia);

			var fileName = $"{_settings.Emisor.Ruc}-07-{serie}-{numero}";
			var solicitudUtc = DateTime.UtcNow;
			var envio = await _facturacionSunatService.EnviarDocumento(fileName, body);

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
				await FacturacionVoucherHelper.ActualizarVoucherPostEnvioAsync(con, voucherId, envio, _pdfLocalService);
				await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, voucherId, "SEND", envio, solicitudUtc);
			}

			return new NotaComprobanteResultadoDto
			{
				Id = voucherId.ToString(),
				Tipo = "NOTA_CREDITO",
				Serie = serie,
				Numero = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				Moneda = request.Moneda,
				EstadoSunat = FacturacionVoucherHelper.MapearEstadoSunatUi(FacturacionVoucherHelper.NormalizarSunatStatusParaVoucher(envio)),
				DocumentId = envio.DocumentId,
				CodigoRespuestaSunat = envio.CodigoRespuestaSunat ?? string.Empty,
				MensajeSunat = envio.MensajeSunat ?? envio.Mensaje ?? string.Empty,
				DetalleError = envio.DetalleError ?? string.Empty,
				Subtotal = subtotal,
				Igv = igv,
				Total = total,
				VoucherReferenciaId = referencia.Id,
				DocumentoReferencia = $"{referencia.Serie}-{referencia.Numero}"
			};
		}

		private void ValidarConfiguracion()
		{
			if (string.IsNullOrWhiteSpace(_settings.Emisor?.Ruc) || string.IsNullOrWhiteSpace(_settings.Emisor.RazonSocial))
			{
				throw new InvalidOperationException("Falta FacturacionSettings:Emisor:Ruc o RazonSocial.");
			}
		}

		private static List<NotaCreditoItemDto> PrepararItems(NotaCreditoEmitirRequestDto request, NotaComprobanteBaseDisponibleDto referencia)
		{
			if (request.Motivo.Codigo is "01" or "02" or "06")
			{
				return referencia.Items.Select(x => new NotaCreditoItemDto
				{
					VoucherItemReferenciaId = x.Id,
					ProductoId = x.ProductoId,
					Codigo = x.Codigo,
					Descripcion = x.Descripcion,
					Cantidad = x.Cantidad,
					PrecioUnitario = x.PrecioUnitario,
					UnidadMedida = x.UnidadMedida
				}).ToList();
			}

			if (request.Motivo.Codigo != "09")
			{
				return request.Items.Select(item =>
				{
					var itemBase = ResolverItemBase(referencia, item)
						?? throw new InvalidOperationException($"No se pudo identificar el ítem del comprobante base. Envíe VoucherItemReferenciaId, ProductoId o Código válidos.");

					return new NotaCreditoItemDto
					{
						VoucherItemReferenciaId = string.IsNullOrWhiteSpace(item.VoucherItemReferenciaId) ? itemBase.Id : item.VoucherItemReferenciaId,
						ProductoId = item.ProductoId ?? itemBase.ProductoId,
						Codigo = string.IsNullOrWhiteSpace(item.Codigo) ? itemBase.Codigo : item.Codigo,
						Descripcion = string.IsNullOrWhiteSpace(item.Descripcion) ? itemBase.Descripcion : item.Descripcion,
						Cantidad = item.Cantidad,
						PrecioUnitario = item.PrecioUnitario > 0 ? item.PrecioUnitario : itemBase.PrecioUnitario,
						UnidadMedida = string.IsNullOrWhiteSpace(item.UnidadMedida) ? itemBase.UnidadMedida : item.UnidadMedida
					};
				}).ToList();
			}

			return request.Items;
		}

		private static List<NotaCreditoItemCalculado> CalcularItems(IEnumerable<NotaCreditoItemDto> items, decimal igvPorcentaje)
		{
			return items.Select(item =>
			{
				var valorVenta = FacturacionVoucherHelper.Redondear(item.Cantidad * item.PrecioUnitario);
				var igv = CodigoAfectacionIgvGravado == "10"
					? FacturacionVoucherHelper.Redondear(valorVenta * igvPorcentaje / 100m)
					: 0m;
				var importe = FacturacionVoucherHelper.Redondear(valorVenta + igv);

				return new NotaCreditoItemCalculado
				{
					VoucherItemReferenciaId = item.VoucherItemReferenciaId,
					ProductoId = item.ProductoId,
					Codigo = item.Codigo,
					Descripcion = item.Descripcion,
					Cantidad = item.Cantidad,
					PrecioUnitario = item.PrecioUnitario,
					ValorVenta = valorVenta,
					Igv = igv,
					Importe = importe,
					UnidadMedida = item.UnidadMedida,
					PorcentajeIgv = igvPorcentaje,
					CodigoAfectacionIgv = CodigoAfectacionIgvGravado
				};
			}).ToList();
		}

		private static void ValidarSolicitud(NotaCreditoEmitirRequestDto request, NotaComprobanteBaseDisponibleDto referencia, List<NotaCreditoItemCalculado> items)
		{
			var motivosValidos = new HashSet<string> { "01", "02", "03", "04", "05", "06", "07", "08", "09" };
			if (!motivosValidos.Contains(request.Motivo.Codigo))
			{
				throw new InvalidOperationException("El código del motivo de nota de crédito no es válido.");
			}

			if (referencia.SunatTypeCode == "03" && request.Motivo.Codigo is "04" or "05" or "08")
			{
				throw new InvalidOperationException("Las notas de crédito 04, 05 y 08 no pueden vincularse a una boleta.");
			}

			if (string.IsNullOrWhiteSpace(request.Motivo.Descripcion))
			{
				throw new InvalidOperationException("La descripción del motivo es obligatoria.");
			}

			if (request.IgvPorcentaje < 0)
			{
				throw new InvalidOperationException("El porcentaje de IGV no puede ser negativo.");
			}

			if (items.Count == 0)
			{
				throw new InvalidOperationException("La nota de crédito debe tener al menos un ítem.");
			}

			foreach (var item in items)
			{
				if (item.Cantidad <= 0 || item.Importe <= 0 || string.IsNullOrWhiteSpace(item.Descripcion))
				{
					throw new InvalidOperationException("Todos los ítems de la nota de crédito deben ser válidos.");
				}

				if (request.Motivo.Codigo != "09")
				{
					var itemBase = ResolverItemBase(referencia, item)
						?? throw new InvalidOperationException($"El ítem '{item.Descripcion}' no pertenece al comprobante base.");

					if (item.Cantidad > itemBase.Cantidad)
					{
						throw new InvalidOperationException($"La cantidad del ítem '{item.Descripcion}' excede la del comprobante base.");
					}

					if (FacturacionVoucherHelper.Redondear(item.Importe) > FacturacionVoucherHelper.Redondear(itemBase.Importe))
					{
						throw new InvalidOperationException($"El importe del ítem '{item.Descripcion}' excede el original.");
					}
				}
			}

			if (FacturacionVoucherHelper.Redondear(items.Sum(x => x.Importe)) > FacturacionVoucherHelper.Redondear(referencia.Total))
			{
				throw new InvalidOperationException("La nota de crédito no puede exceder el total del comprobante base.");
			}
		}

		private async Task InsertarVoucherAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, NotaComprobanteBaseDisponibleDto referencia, DateTime fechaEmision, string serie, string numero, string moneda, decimal subtotal, decimal igv, decimal total)
		{
			const string sql = """
				INSERT INTO dbo.Voucher
				(
					Id, VentaId, SunatTypeCode, Series, Number, IssuerRuc, IssuerLegalName, IssueDate, Currency, Subtotal, Igv, Total, SunatStatus
				)
				SELECT
					@Id, VentaId, '07', @Series, @Number, @IssuerRuc, @IssuerLegalName, @IssueDate, @Currency, @Subtotal, @Igv, @Total, 'NO_ENVIADO'
				FROM dbo.Voucher
				WHERE Id = @ReferencedVoucherId;
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.AddWithValue("@ReferencedVoucherId", Guid.Parse(referencia.Id));
			cmd.Parameters.AddWithValue("@Series", serie);
			cmd.Parameters.AddWithValue("@Number", numero);
			cmd.Parameters.AddWithValue("@IssuerRuc", _settings.Emisor.Ruc);
			cmd.Parameters.AddWithValue("@IssuerLegalName", _settings.Emisor.RazonSocial);
			cmd.Parameters.AddWithValue("@IssueDate", fechaEmision);
			cmd.Parameters.AddWithValue("@Currency", moneda);
			cmd.Parameters.AddWithValue("@Subtotal", subtotal);
			cmd.Parameters.AddWithValue("@Igv", igv);
			cmd.Parameters.AddWithValue("@Total", total);
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task InsertarItemsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, List<NotaCreditoItemCalculado> items, NotaComprobanteBaseDisponibleDto referencia)
		{
			const string sql = """
				INSERT INTO dbo.VoucherItem
				(
					Id, VoucherId, ReferencedVoucherItemId, LineNumber, ProductId, ProductCode, Description, Quantity, UnitCode, UnitPrice, SaleValue, IgvPercentage, Igv, Total
				)
				VALUES
				(
					@Id, @VoucherId, @ReferencedVoucherItemId, @LineNumber, @ProductId, @ProductCode, @Description, @Quantity, @UnitCode, @UnitPrice, @SaleValue, @IgvPercentage, @Igv, @Total
				);
				""";

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var itemBase = ResolverItemBase(referencia, item);
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@ReferencedVoucherItemId", itemBase is null ? DBNull.Value : Guid.Parse(itemBase.Id));
				cmd.Parameters.AddWithValue("@LineNumber", i + 1);
				cmd.Parameters.AddWithValue("@ProductId", (object?)item.ProductoId ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@ProductCode", string.IsNullOrWhiteSpace(item.Codigo) ? DBNull.Value : item.Codigo);
				cmd.Parameters.AddWithValue("@Description", item.Descripcion);
				cmd.Parameters.AddWithValue("@Quantity", item.Cantidad);
				cmd.Parameters.AddWithValue("@UnitCode", item.UnidadMedida);
				cmd.Parameters.AddWithValue("@UnitPrice", item.PrecioUnitario);
				cmd.Parameters.AddWithValue("@SaleValue", item.ValorVenta);
				cmd.Parameters.AddWithValue("@IgvPercentage", item.PorcentajeIgv);
				cmd.Parameters.AddWithValue("@Igv", item.Igv);
				cmd.Parameters.AddWithValue("@Total", item.Importe);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private static NotaComprobanteBaseItemDto? ResolverItemBase(NotaComprobanteBaseDisponibleDto referencia, INotaItemReferencia item)
			=> ResolverItemBase(referencia, item.VoucherItemReferenciaId, item.ProductoId, item.Codigo, item.Descripcion);

		private static NotaComprobanteBaseItemDto? ResolverItemBase(NotaComprobanteBaseDisponibleDto referencia, NotaCreditoItemDto item)
			=> ResolverItemBase(referencia, item.VoucherItemReferenciaId, item.ProductoId, item.Codigo, item.Descripcion);

		private static NotaComprobanteBaseItemDto? ResolverItemBase(
			NotaComprobanteBaseDisponibleDto referencia,
			string? voucherItemReferenciaId,
			int? productoId,
			string? codigo,
			string descripcion)
		{
			if (!string.IsNullOrWhiteSpace(voucherItemReferenciaId))
			{
				var referenciaNormalizada = voucherItemReferenciaId.Trim();
				if (Guid.TryParse(referenciaNormalizada, out var referenciaGuid))
				{
					return referencia.Items.FirstOrDefault(x =>
						Guid.TryParse(x.Id, out var itemGuid) &&
						itemGuid == referenciaGuid);
				}

				return referencia.Items.FirstOrDefault(x =>
					string.Equals((x.Id ?? string.Empty).Trim(), referenciaNormalizada, StringComparison.OrdinalIgnoreCase));
			}

			if (productoId.HasValue)
			{
				return referencia.Items.FirstOrDefault(x => x.ProductoId == productoId);
			}

			if (!string.IsNullOrWhiteSpace(codigo))
			{
				var codigoNormalizado = codigo.Trim();
				return referencia.Items.FirstOrDefault(x =>
					string.Equals((x.Codigo ?? string.Empty).Trim(), codigoNormalizado, StringComparison.OrdinalIgnoreCase));
			}

			if (string.IsNullOrWhiteSpace(descripcion))
			{
				return null;
			}

			var descripcionNormalizada = descripcion.Trim();
			return referencia.Items.FirstOrDefault(x =>
				string.Equals((x.Descripcion ?? string.Empty).Trim(), descripcionNormalizada, StringComparison.OrdinalIgnoreCase));
		}

		private static UblItemPayloadDto MapearItemUbl(NotaCreditoItemCalculado item) => new()
		{
			Descripcion = item.Descripcion,
			Cantidad = item.Cantidad,
			PrecioUnitario = item.PrecioUnitario,
			ValorVenta = item.ValorVenta,
			Igv = item.Igv,
			Importe = item.Importe,
			PrecioConIgv = item.Cantidad <= 0 ? 0 : FacturacionVoucherHelper.Redondear(item.Importe / item.Cantidad),
			UnidadMedida = item.UnidadMedida,
			PorcentajeIgv = item.PorcentajeIgv,
			CodigoAfectacionIgv = item.CodigoAfectacionIgv
		};

		private interface INotaItemReferencia
		{
			string? VoucherItemReferenciaId { get; }
			int? ProductoId { get; }
			string? Codigo { get; }
			string Descripcion { get; }
		}

		private sealed class NotaCreditoItemCalculado : INotaItemReferencia
		{
			public string? VoucherItemReferenciaId { get; init; }
			public int? ProductoId { get; init; }
			public string? Codigo { get; init; }
			public string Descripcion { get; init; } = string.Empty;
			public decimal Cantidad { get; init; }
			public decimal PrecioUnitario { get; init; }
			public decimal ValorVenta { get; init; }
			public decimal Igv { get; init; }
			public decimal Importe { get; init; }
			public string UnidadMedida { get; init; } = "NIU";
			public decimal PorcentajeIgv { get; init; }
			public string CodigoAfectacionIgv { get; init; } = "10";
		}
	}
}
