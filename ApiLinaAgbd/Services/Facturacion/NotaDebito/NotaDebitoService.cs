using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Models.Facturacion.Notas;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using ApiLinaAgbd.Repositories.Facturacion.NotaDebito;
using ApiLinaAgbd.Services.Facturacion.Shared;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion.NotaDebito
{
	public class NotaDebitoService : INotaDebitoService
	{
		private const string CodigoAfectacionIgvGravado = "10";
		private const string UnidadMedidaServicio = "ZZ";

		private const string SerieFactura = "FD01";
		private const string SerieBoleta = "BD01";
		private readonly INotaDebitoRepository _repository;
		private readonly NotaDebitoUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionPdfLocalService _pdfLocalService;
		private readonly FacturacionSettings _settings;

		public NotaDebitoService(
			INotaDebitoRepository repository,
			NotaDebitoUblBuilder builder,
			FacturacionSunatService facturacionSunatService,
			FacturacionPdfLocalService pdfLocalService,
			IOptions<FacturacionSettings> options)
		{
			_repository = repository;
			_builder = builder;
			_facturacionSunatService = facturacionSunatService;
			_pdfLocalService = pdfLocalService;
			_settings = options.Value;
		}

		public async Task<NotaComprobanteResultadoDto> EmitirAsync(NotaDebitoEmitirRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(_settings.Emisor?.Ruc) || string.IsNullOrWhiteSpace(_settings.Emisor.RazonSocial))
			{
				throw new InvalidOperationException("Falta FacturacionSettings:Emisor:Ruc o RazonSocial.");
			}

			var referencia = (await _repository.ListarComprobantesBaseAsync()).FirstOrDefault(x => x.Id == request.VoucherReferenciaId)
				?? throw new InvalidOperationException("El comprobante base no existe.");
			if (referencia.SunatTypeCode is not ("01" or "03"))
				throw new InvalidOperationException("Una nota de débito solo puede referenciar una factura o boleta.");
			if (request.Motivo is null || request.Motivo.Codigo is not ("01" or "02"))
				throw new InvalidOperationException("La nota de débito solo admite los motivos 01 (intereses por mora) y 02 (aumento en el valor).");
			if (request.Moneda is not ("PEN" or "USD") || !string.Equals(request.Moneda, referencia.Moneda, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("La moneda de la nota debe coincidir con la del comprobante base y ser PEN o USD.");

			var items = PrepararItems(request, referencia);
			var itemsCalculados = CalcularItems(items, request.IgvPorcentaje);
			ValidarSolicitud(request, referencia, itemsCalculados);
			var subtotal = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.ValorVenta));
			var igv = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.Igv));
			var total = FacturacionVoucherHelper.Redondear(itemsCalculados.Sum(x => x.Importe));
			var fechaEmision = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Lima").Date;
			var serie = referencia.SunatTypeCode == "01" ? SerieFactura : SerieBoleta;
			var voucherId = Guid.NewGuid();
			string numero;

			using (var con = _repository.CreateConnection())
			{
				await con.OpenAsync();
				using var tx = con.BeginTransaction();

				numero = await GenerarNumeroAsync(con, tx, serie);
				await InsertarVoucherAsync(con, tx, voucherId, referencia, fechaEmision, serie, numero, request.Moneda, subtotal, igv, total);
				await FacturacionVoucherHelper.InsertarPartyAsync(con, tx, voucherId, "CUSTOMER", referencia.ClienteTipoDocumento, referencia.ClienteDocumento, referencia.ClienteNombre, referencia.ClienteDireccion);
				await InsertarItemsAsync(con, tx, voucherId, itemsCalculados, referencia);
				await FacturacionVoucherHelper.InsertarAdjustmentAsync(con, tx, voucherId, Guid.Parse(referencia.Id), request.Motivo.Codigo, request.Motivo.Descripcion);
				await FacturacionVoucherHelper.InsertarObservacionesAsync(con, tx, voucherId, request.Observaciones);
				await InsertarMetadataAsync(con, tx, voucherId, request.SolicitudId, fechaEmision);
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
			});

			var fileName = $"{_settings.Emisor.Ruc}-08-{serie}-{numero}";
			var solicitudUtc = DateTime.UtcNow;
			var envio = await _facturacionSunatService.EnviarDocumento(fileName, body);

			if (!FacturacionVoucherHelper.FueRecibidoPorApi(envio))
			{
				using var conFallo = _repository.CreateConnection();
				await conFallo.OpenAsync();
				await FacturacionVoucherHelper.ActualizarVoucherPostFalloComunicacionAsync(conFallo, voucherId);
				await FacturacionVoucherHelper.RegistrarTransmisionAsync(conFallo, voucherId, "SEND", envio, solicitudUtc);
				return CrearResultado(voucherId, referencia, serie, numero, fechaEmision, request.Moneda, subtotal, igv, total, envio, "PENDIENTE_ENVIO");
			}

			using (var con = _repository.CreateConnection())
			{
				await con.OpenAsync();
				var estadoPostEnvio = await FacturacionVoucherHelper.ConsultarEstadoLuegoDeEnviarAsync(_facturacionSunatService, envio);
				await FacturacionVoucherHelper.ActualizarVoucherPostEnvioAsync(con, voucherId, estadoPostEnvio.ResultadoFinal, _pdfLocalService);
				await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, voucherId, "SEND", envio, solicitudUtc);
				if (estadoPostEnvio.Consulta is not null)
				{
					await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, voucherId, "STATUS_QUERY", estadoPostEnvio.Consulta, DateTime.UtcNow);
				}
			}

			return CrearResultado(voucherId, referencia, serie, numero, fechaEmision, request.Moneda, subtotal, igv, total, envio, null);
		}

		public Task<List<NotaComprobanteBaseDisponibleDto>> ListarBasesAsync() => _repository.ListarComprobantesBaseAsync();

		private NotaComprobanteResultadoDto CrearResultado(Guid voucherId, NotaComprobanteBaseDisponibleDto referencia, string serie, string numero, DateTime fechaEmision, string moneda, decimal subtotal, decimal igv, decimal total, FacturacionEnvioResultado envio, string? estadoForzado)
		{
			return new NotaComprobanteResultadoDto
			{
				Id = voucherId.ToString(),
				Tipo = "NOTA_DEBITO",
				Serie = serie,
				Numero = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
					Moneda = moneda,
				EstadoSunat = estadoForzado ?? FacturacionVoucherHelper.MapearEstadoSunatUi(FacturacionVoucherHelper.NormalizarSunatStatusParaVoucher(envio)),
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

		private static List<NotaDebitoItemEmitirDto> PrepararItems(NotaDebitoEmitirRequestDto request, NotaComprobanteBaseDisponibleDto referencia)
		{
			return request.Items.Select(item =>
			{
				var itemBase = request.Motivo.Codigo == "01" ? null : ResolverItemBase(referencia, item);
				if (string.Equals(item.Ambito, "ITEM", StringComparison.OrdinalIgnoreCase) && itemBase is null)
					throw new InvalidOperationException("El ítem indicado no pertenece al comprobante base.");
				var esAbstracto = string.Equals(item.Ambito, "COMPROBANTE", StringComparison.OrdinalIgnoreCase) || itemBase is null;
				var monto = item.MontoAdicionalSinIgv ?? item.PrecioUnitario;
				if (monto <= 0) throw new InvalidOperationException("Cada ítem debe indicar un monto adicional sin IGV mayor que cero.");

				return new NotaDebitoItemEmitirDto
				{
					Ambito = esAbstracto ? "COMPROBANTE" : "ITEM",
					VoucherItemReferenciaId = esAbstracto ? null : itemBase!.Id,
					ProductoId = esAbstracto ? null : itemBase!.ProductoId,
					Codigo = esAbstracto ? null : itemBase!.Codigo,
					Descripcion = string.IsNullOrWhiteSpace(item.Descripcion)
						? request.Motivo.Codigo == "01"
							? $"Intereses moratorios por pago fuera de fecha de {(referencia.SunatTypeCode == "01" ? "la Factura" : "la Boleta")} {referencia.Serie}-{referencia.Numero}"
							: "Ajuste en el valor del comprobante"
						: item.Descripcion.Trim(),
					Cantidad = esAbstracto ? 1m : item.Cantidad,
					PrecioUnitario = monto,
					UnidadMedida = esAbstracto ? UnidadMedidaServicio : (string.IsNullOrWhiteSpace(item.UnidadMedida) ? itemBase!.UnidadMedida : item.UnidadMedida)
				};
			}).ToList();
		}

		private static List<NotaDebitoItemCalculado> CalcularItems(IEnumerable<NotaDebitoItemEmitirDto> items, decimal igvPorcentaje)
		{
			return items.Select(item =>
			{
				var valorVenta = FacturacionVoucherHelper.Redondear(item.Cantidad * item.PrecioUnitario);
				var igv = CodigoAfectacionIgvGravado == "10"
					? FacturacionVoucherHelper.Redondear(valorVenta * igvPorcentaje / 100m)
					: 0m;
				var importe = FacturacionVoucherHelper.Redondear(valorVenta + igv);

				return new NotaDebitoItemCalculado
				{
					Ambito = item.Ambito,
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

		private static void ValidarSolicitud(NotaDebitoEmitirRequestDto request, NotaComprobanteBaseDisponibleDto referencia, List<NotaDebitoItemCalculado> items)
		{
			if (string.IsNullOrWhiteSpace(request.Motivo.Descripcion))
			{
				throw new InvalidOperationException("La descripción del motivo es obligatoria.");
			}

			if (request.IgvPorcentaje < 0)
			{
				throw new InvalidOperationException("El porcentaje de IGV no puede ser negativo.");
			}

			foreach (var item in items)
			{
				if (item.Cantidad <= 0 || item.Importe <= 0 || string.IsNullOrWhiteSpace(item.Descripcion))
				{
					throw new InvalidOperationException("Todos los ítems de la nota de débito deben ser válidos.");
				}

				var baseItem = string.Equals(item.Ambito, "ITEM", StringComparison.OrdinalIgnoreCase)
					? ResolverItemBase(referencia, item)
					: null;
				if (string.Equals(item.Ambito, "ITEM", StringComparison.OrdinalIgnoreCase) && baseItem is null)
					throw new InvalidOperationException("El ítem indicado no pertenece al comprobante base.");
				if (baseItem is not null && item.Cantidad > baseItem.Cantidad)
				{
					throw new InvalidOperationException($"La cantidad del ítem '{item.Descripcion}' excede la del comprobante base.");
				}
			}
		}

		private async Task<string> GenerarNumeroAsync(SqlConnection con, SqlTransaction tx, string serie)
		{
			const string sql = """
			SELECT ISNULL(MAX(TRY_CONVERT(INT, Number)), 0) + 1
			FROM dbo.Voucher WITH (UPDLOCK, HOLDLOCK)
			WHERE SunatTypeCode = '08' AND Series = @Series AND IssuerRuc = @Ruc;
			""";
			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Series", serie);
			cmd.Parameters.AddWithValue("@Ruc", _settings.Emisor.Ruc);
			var next = Convert.ToInt32(await cmd.ExecuteScalarAsync());
			if (next <= 0 || next > 99_999_999) throw new InvalidOperationException("No quedan correlativos disponibles para la nota de débito.");
			return next.ToString("D8");
		}

		private static async Task InsertarMetadataAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, string? solicitudId, DateTime fecha)
		{
			if (!Guid.TryParse(solicitudId, out var solicitudGuid)) return;
			const string sql = """
			IF OBJECT_ID(N'dbo.NotaDebitoMetadata', N'U') IS NOT NULL
			BEGIN
				INSERT INTO dbo.NotaDebitoMetadata (VoucherId, SolicitudId, EmissionTime)
				VALUES (@VoucherId, @SolicitudId, @EmissionTime);
			END
			""";
			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@SolicitudId", solicitudGuid);
			cmd.Parameters.AddWithValue("@EmissionTime", fecha.TimeOfDay);
			await cmd.ExecuteNonQueryAsync();
		}

		private async Task InsertarVoucherAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, NotaComprobanteBaseDisponibleDto referencia, DateTime fechaEmision, string serie, string numero, string moneda, decimal subtotal, decimal igv, decimal total)
		{
			const string sql = """
				INSERT INTO dbo.Voucher
				(
					Id, VentaId, SunatTypeCode, Series, Number, IssuerRuc, IssuerLegalName, IssueDate, Currency, Subtotal, Igv, Total, SunatStatus
				)
				SELECT
					@Id, VentaId, '08', @Series, @Number, @IssuerRuc, @IssuerLegalName, @IssueDate, @Currency, @Subtotal, @Igv, @Total, 'NO_ENVIADO'
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

		private static async Task InsertarItemsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, List<NotaDebitoItemCalculado> items, NotaComprobanteBaseDisponibleDto referencia)
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
				var itemBase = string.Equals(item.Ambito, "ITEM", StringComparison.OrdinalIgnoreCase)
					? ResolverItemBase(referencia, item)
					: null;
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

		private static NotaComprobanteBaseItemDto? ResolverItemBase(NotaComprobanteBaseDisponibleDto referencia, NotaDebitoItemEmitirDto item)
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

		private static UblItemPayloadDto MapearItemUbl(NotaDebitoItemCalculado item) => new()
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

		private sealed class NotaDebitoItemCalculado : INotaItemReferencia
		{
			public string Ambito { get; init; } = "COMPROBANTE";
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
