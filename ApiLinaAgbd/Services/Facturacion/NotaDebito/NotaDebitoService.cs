using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Models.Facturacion.Notas;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using ApiLinaAgbd.Repositories.Facturacion.NotaDebito;
using ApiLinaAgbd.Services.Facturacion.NotaCredito;
using ApiLinaAgbd.Services.Facturacion.Shared;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion.NotaDebito
{
	public class NotaDebitoService : INotaDebitoService
	{
		private const string CodigoAfectacionIgvGravado = "10";
		private const string DescripcionPenalidadDefault = "Penalidad por cambio posterior a la venta";
		private const string UnidadMedidaDefault = "NIU";

		private const string SerieFactura = "FD01";
		private const string SerieBoleta = "BD01";
		private readonly INotaDebitoRepository _repository;
		private readonly INotaCreditoService _notaCreditoService;
		private readonly NotaDebitoUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionPdfLocalService _pdfLocalService;
		private readonly FacturacionSettings _settings;

		public NotaDebitoService(
			INotaDebitoRepository repository,
			INotaCreditoService notaCreditoService,
			NotaDebitoUblBuilder builder,
			FacturacionSunatService facturacionSunatService,
			FacturacionPdfLocalService pdfLocalService,
			IOptions<FacturacionSettings> options)
		{
			_repository = repository;
			_notaCreditoService = notaCreditoService;
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

			var referencia = (await _notaCreditoService.ListarComprobantesBaseAsync()).FirstOrDefault(x => x.Id == request.VoucherReferenciaId)
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

			using (var con = _repository.CreateConnection())
			{
				await con.OpenAsync();
				using var tx = con.BeginTransaction();

				numero = await FacturacionVoucherHelper.GenerarNumeroAleatorioDisponibleAsync(con, tx, "08", serie, _settings.Emisor.Ruc);
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
				throw new InvalidOperationException(envio.DetalleError ?? envio.MensajeSunat ?? envio.Mensaje);
			}

			using (var con = _repository.CreateConnection())
			{
				await con.OpenAsync();
				await FacturacionVoucherHelper.ActualizarVoucherPostEnvioAsync(con, voucherId, envio, _pdfLocalService);
				await FacturacionVoucherHelper.RegistrarTransmisionAsync(con, voucherId, "SEND", envio, solicitudUtc);
			}

			return new NotaComprobanteResultadoDto
			{
				Id = voucherId.ToString(),
				Tipo = "NOTA_DEBITO",
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

		private static List<NotaDebitoItemEmitirDto> PrepararItems(NotaDebitoEmitirRequestDto request, NotaComprobanteBaseDisponibleDto referencia)
		{
			if (request.Motivo.Codigo == "03")
			{
				return request.Items.Select(item => new NotaDebitoItemEmitirDto
				{
					Descripcion = string.IsNullOrWhiteSpace(item.Descripcion)
						? DescripcionPenalidadDefault
						: item.Descripcion.Trim(),
					Cantidad = 1m,
					PrecioUnitario = item.PrecioUnitario,
					UnidadMedida = UnidadMedidaDefault
				}).ToList();
			}

			return request.Items.Select(item =>
			{
				var itemBase = ResolverItemBase(referencia, item)
					?? throw new InvalidOperationException("No se pudo identificar el ítem del comprobante base. Envíe VoucherItemReferenciaId, ProductoId o Código válidos.");

				return new NotaDebitoItemEmitirDto
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
			if (request.Motivo.Codigo is not ("01" or "02" or "03" or "11"))
			{
				throw new InvalidOperationException("El código del motivo de nota de débito no es válido.");
			}

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

				if (request.Motivo.Codigo == "03")
				{
					if (item.Cantidad != 1m)
					{
						throw new InvalidOperationException("La nota de débito por penalidad siempre debe usar cantidad 1.");
					}

					continue;
				}

				var baseItem = ResolverItemBase(referencia, item);
				if (baseItem is not null && item.Cantidad > baseItem.Cantidad)
				{
					throw new InvalidOperationException($"La cantidad del ítem '{item.Descripcion}' excede la del comprobante base.");
				}
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
