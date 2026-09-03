using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Models.Facturacion.Notas;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using ApiLinaAgbd.Services;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class LiquidacionCompraService
	{
		private const string SerieLiquidacion = "LC01";
		private readonly Conexion _conexion;
		private readonly LiquidacionCompraUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionPdfLocalService _pdfLocalService;
		private readonly FacturacionSettings _settings;

		public LiquidacionCompraService(
			Conexion conexion,
			LiquidacionCompraUblBuilder builder,
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

		public async Task<List<LiquidacionCompraDisponibleDto>> ListarComprasDisponiblesAsync()
		{
			var compras = new Dictionary<int, LiquidacionCompraDisponibleDto>();

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();

			const string sql = """
				SELECT
					c.id AS CompraId,
					c.fecha_compra,
					pr.ruc,
					pr.razon_social,
					pr.nombre_contacto AS NombreContacto,
					dir.id_distrito AS DistritoId,
					dir.nombre_direccion AS Direccion,
					di.nombre AS Distrito,
					pv.nombre AS Provincia,
					dep.nombre AS Departamento,
					p.id AS ProductoId,
					p.codigo AS CodigoProducto,
					COALESCE(NULLIF(p.descripcion, ''), p.nombre) AS DescripcionProducto,
					CAST(dc.cantidad AS decimal(18, 2)) AS Cantidad,
					CAST(dc.costo_total AS decimal(18, 2)) AS CostoTotal,
					COALESCE(NULLIF(um.abreviatura, ''), 'NIU') AS UnidadMedida
				FROM dbo.compra c
				INNER JOIN dbo.proveedor pr ON pr.id = c.id_proveedor
				LEFT JOIN dbo.direccion dir ON dir.id = pr.id_direccion
				LEFT JOIN dbo.distrito di ON di.id = dir.id_distrito
				LEFT JOIN dbo.provincia pv ON pv.id = di.idprovincia
				LEFT JOIN dbo.departamento dep ON dep.id = pv.iddepartamento
				INNER JOIN dbo.detallecompra dc ON dc.id_compra = c.id
				INNER JOIN dbo.producto p ON p.id = dc.id_producto
				LEFT JOIN dbo.unidadmedida um ON um.id = p.id_unidad_medida
				WHERE NOT EXISTS (
					SELECT 1
					FROM dbo.Voucher v
					WHERE v.CompraId = c.id
					  AND v.SunatTypeCode = '04'
					  AND ISNULL(v.SunatStatus, 'NO_ENVIADO') IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO')
				)
				ORDER BY c.id DESC, dc.id ASC;
				""";

			using var cmd = new SqlCommand(sql, con);
			using var dr = await cmd.ExecuteReaderAsync();
			while (await dr.ReadAsync())
			{
				var compraId = Convert.ToInt32(dr["CompraId"]);
				if (!compras.TryGetValue(compraId, out var compra))
				{
					compra = new LiquidacionCompraDisponibleDto
					{
						CompraId = compraId,
						Codigo = $"COM-{compraId:D6}",
						FechaCompra = Convert.ToDateTime(dr["fecha_compra"]).ToString("yyyy-MM-dd"),
						Vendedor = new LiquidacionCompraVendedorDto
						{
							TipoDocumento = "RUC",
							NumeroDocumento = dr["ruc"]?.ToString() ?? string.Empty,
							Nombre = dr["razon_social"]?.ToString() ?? string.Empty,
							NombreContacto = dr["NombreContacto"]?.ToString()
						},
						UbicacionVendedor = dr["DistritoId"] == DBNull.Value
							? null
							: new LiquidacionCompraUbicacionDisponibleDto
							{
								DistritoId = Convert.ToInt32(dr["DistritoId"]),
								Departamento = dr["Departamento"]?.ToString() ?? string.Empty,
								Provincia = dr["Provincia"]?.ToString() ?? string.Empty,
								Distrito = dr["Distrito"]?.ToString() ?? string.Empty,
								Direccion = dr["Direccion"]?.ToString() ?? string.Empty
							}
					};

					compras.Add(compraId, compra);
				}

				var cantidad = Convert.ToDecimal(dr["Cantidad"]);
				var costoTotal = Convert.ToDecimal(dr["CostoTotal"]);
				var precioUnitario = cantidad <= 0 ? 0 : FacturacionVoucherHelper.Redondear(costoTotal / cantidad);
				var valorVenta = costoTotal;
				var igv = FacturacionVoucherHelper.Redondear(valorVenta * 0.18m);

				compra.Detalle.Add(new LiquidacionCompraDetalleDto
				{
					ProductoId = Convert.ToInt32(dr["ProductoId"]),
					Codigo = dr["CodigoProducto"]?.ToString() ?? string.Empty,
					Descripcion = dr["DescripcionProducto"]?.ToString() ?? string.Empty,
					Cantidad = cantidad,
					PrecioUnitario = precioUnitario,
					ValorVenta = valorVenta,
					Igv = igv,
					Importe = FacturacionVoucherHelper.Redondear(valorVenta + igv),
					UnidadMedida = dr["UnidadMedida"]?.ToString() ?? "NIU"
				});
			}

			foreach (var compra in compras.Values)
			{
				compra.Subtotal = FacturacionVoucherHelper.Redondear(compra.Detalle.Sum(x => x.ValorVenta));
				compra.Igv = FacturacionVoucherHelper.Redondear(compra.Detalle.Sum(x => x.Igv));
				compra.Total = FacturacionVoucherHelper.Redondear(compra.Subtotal + compra.Igv);
			}

			return compras.Values.ToList();
		}

		public async Task<NotaComprobanteResultadoDto> EmitirAsync(LiquidacionCompraEmitirRequestDto request)
		{
			ValidarConfiguracion();

			var compra = (await ListarComprasDisponiblesAsync()).FirstOrDefault(x => x.CompraId == request.CompraOrigenId)
				?? throw new InvalidOperationException("La compra seleccionada no está disponible para liquidación.");

			ValidarSolicitud(request, compra);

			var fechaEmision = FacturacionVoucherHelper.ParsearFechaObligatoria(request.FechaEmision, "La fecha de emisión no es válida.");
			var moneda = (request.Moneda ?? "PEN").Trim().ToUpperInvariant();
			var ubicacionVendedor = await ResolverUbicacionAsync(request.UbicacionVendedor);
			var puntoVenta = await ResolverUbicacionAsync(request.PuntoVenta);
			var voucherId = Guid.NewGuid();
			string numero;

			using (var con = _conexion.ObtenerConexion())
			{
				await con.OpenAsync();
				using var tx = con.BeginTransaction();

				numero = await FacturacionVoucherHelper.GenerarNumeroAleatorioDisponibleAsync(
					con,
					tx,
					LiquidacionCompraUblBuilder.TipoDocumentoLiquidacionCompra,
					SerieLiquidacion,
					_settings.Emisor.Ruc);

				await InsertarVoucherAsync(con, tx, voucherId, compra, fechaEmision, moneda, numero);
				await FacturacionVoucherHelper.InsertarPartyAsync(
					con,
					tx,
					voucherId,
					"SELLER",
					request.Vendedor.TipoDocumento,
					request.Vendedor.NumeroDocumento,
					request.Vendedor.Nombre,
					ubicacionVendedor.Direccion);
				await FacturacionVoucherHelper.InsertarUbicacionAsync(
					con,
					tx,
					voucherId,
					"SELLER_ADDRESS",
					ubicacionVendedor.DistritoId,
					ubicacionVendedor.Direccion);
				await FacturacionVoucherHelper.InsertarUbicacionAsync(
					con,
					tx,
					voucherId,
					"POINT_OF_SALE",
					puntoVenta.DistritoId,
					puntoVenta.Direccion,
					puntoVenta.CodigoEstablecimiento);
				await InsertarItemsAsync(con, tx, voucherId, compra.Detalle);
				await FacturacionVoucherHelper.InsertarObservacionesAsync(con, tx, voucherId, request.Observaciones);
				tx.Commit();
			}

			var body = _builder.Build(new UblInvoicePayloadDto
			{
				Serie = SerieLiquidacion,
				Correlativo = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				HoraEmision = request.HoraEmision,
				Moneda = moneda,
				MontoEnLetras = MontoEnLetras.EnSoles(compra.Total),
				Cliente = new UblPartyPayloadDto
				{
					TipoDocumento = FacturacionVoucherHelper.MapearTipoDocumentoSunat(request.Vendedor.TipoDocumento, false),
					NumeroDocumento = request.Vendedor.NumeroDocumento,
					Nombre = request.Vendedor.Nombre,
					Direccion = ubicacionVendedor.Direccion,
					CodigoUbigeo = ubicacionVendedor.CodigoUbigeo,
					Departamento = ubicacionVendedor.Departamento,
					Provincia = ubicacionVendedor.Provincia,
					Distrito = ubicacionVendedor.Distrito
				},
				Totales = new UblTotalsPayloadDto
				{
					ValorVenta = compra.Subtotal,
					Igv = compra.Igv,
					Total = compra.Total
				},
				Items = compra.Detalle.Select(x => new UblItemPayloadDto
				{
					Codigo = x.Codigo,
					Descripcion = x.Descripcion,
					Cantidad = x.Cantidad,
					PrecioUnitario = x.PrecioUnitario,
					ValorVenta = x.ValorVenta,
					Igv = x.Igv,
					PrecioConIgv = x.Cantidad <= 0 ? 0 : FacturacionVoucherHelper.Redondear(x.Importe / x.Cantidad),
					UnidadMedida = x.UnidadMedida
				}).ToList()
			}, puntoVenta);
			var fileName = $"{_settings.Emisor.Ruc}-{LiquidacionCompraUblBuilder.TipoDocumentoLiquidacionCompra}-{SerieLiquidacion}-{numero}";
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
				Tipo = "LIQUIDACION_COMPRA",
				Serie = SerieLiquidacion,
				Numero = numero,
				FechaEmision = fechaEmision.ToString("yyyy-MM-dd"),
				Moneda = moneda,
				EstadoSunat = FacturacionVoucherHelper.MapearEstadoSunatUi(FacturacionVoucherHelper.NormalizarSunatStatusParaVoucher(envio)),
				DocumentId = envio.DocumentId,
				CodigoRespuestaSunat = envio.CodigoRespuestaSunat ?? string.Empty,
				MensajeSunat = envio.MensajeSunat ?? envio.Mensaje ?? string.Empty,
				DetalleError = envio.DetalleError ?? string.Empty,
				Subtotal = compra.Subtotal,
				Igv = compra.Igv,
				Total = compra.Total,
				VoucherReferenciaId = compra.CompraId.ToString(),
				DocumentoReferencia = compra.Codigo
			};
		}

		private async Task<LiquidacionCompraUbicacionDto> ResolverUbicacionAsync(LiquidacionCompraUbicacionDto ubicacion)
		{
			const string sql = """
				SELECT TOP (1)
					di.id,
					di.codigo_ubigeo,
					di.nombre AS Distrito,
					pv.nombre AS Provincia,
					dep.nombre AS Departamento
				FROM dbo.distrito di
				INNER JOIN dbo.provincia pv ON pv.id = di.idprovincia
				INNER JOIN dbo.departamento dep ON dep.id = pv.iddepartamento
				WHERE (@DistritoId > 0 AND di.id = @DistritoId)
				   OR (@DistritoId <= 0 AND @CodigoUbigeo <> '' AND di.codigo_ubigeo = @CodigoUbigeo);
				""";

			using var con = _conexion.ObtenerConexion();
			await con.OpenAsync();
			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@DistritoId", ubicacion.DistritoId);
			cmd.Parameters.AddWithValue("@CodigoUbigeo", (ubicacion.CodigoUbigeo ?? string.Empty).Trim());
			using var dr = await cmd.ExecuteReaderAsync();

			if (!await dr.ReadAsync())
			{
				throw new InvalidOperationException("No se encontró la ubicación indicada para la liquidación.");
			}

			return new LiquidacionCompraUbicacionDto
			{
				DistritoId = Convert.ToInt32(dr["id"]),
				Direccion = ubicacion.Direccion,
				CodigoEstablecimiento = ubicacion.CodigoEstablecimiento,
				CodigoUbigeo = dr["codigo_ubigeo"]?.ToString(),
				Departamento = dr["Departamento"]?.ToString(),
				Provincia = dr["Provincia"]?.ToString(),
				Distrito = dr["Distrito"]?.ToString()
			};
		}

		private void ValidarConfiguracion()
		{
			if (string.IsNullOrWhiteSpace(_settings.Emisor?.Ruc) || string.IsNullOrWhiteSpace(_settings.Emisor.RazonSocial))
			{
				throw new InvalidOperationException("Falta FacturacionSettings:Emisor:Ruc o RazonSocial.");
			}
		}

		private static void ValidarSolicitud(LiquidacionCompraEmitirRequestDto request, LiquidacionCompraDisponibleDto compra)
		{
			var moneda = (request.Moneda ?? "PEN").Trim().ToUpperInvariant();
			if (moneda is not ("PEN" or "USD"))
			{
				throw new InvalidOperationException("La moneda permitida es PEN o USD.");
			}

			if (!FacturacionVoucherHelper.DocumentoValido(request.Vendedor.TipoDocumento, request.Vendedor.NumeroDocumento))
			{
				throw new InvalidOperationException("El documento del vendedor no cumple el formato esperado.");
			}

			if ((request.Vendedor.TipoDocumento ?? string.Empty).Trim().ToUpperInvariant() is not ("DNI" or "CE"))
			{
				throw new InvalidOperationException("La liquidación de compra solo permite DNI o CE para el vendedor.");
			}

			if (string.IsNullOrWhiteSpace(request.Vendedor.Nombre))
			{
				throw new InvalidOperationException("El nombre del vendedor es obligatorio para la liquidación.");
			}

			if ((request.UbicacionVendedor.DistritoId <= 0 && string.IsNullOrWhiteSpace(request.UbicacionVendedor.CodigoUbigeo))
				|| string.IsNullOrWhiteSpace(request.UbicacionVendedor.Direccion))
			{
				throw new InvalidOperationException("La ubicación del vendedor es obligatoria.");
			}

			if ((request.PuntoVenta.DistritoId <= 0 && string.IsNullOrWhiteSpace(request.PuntoVenta.CodigoUbigeo))
				|| string.IsNullOrWhiteSpace(request.PuntoVenta.Direccion))
			{
				throw new InvalidOperationException("El punto de venta es obligatorio.");
			}

			if (compra.Detalle.Count == 0)
			{
				throw new InvalidOperationException("La compra no tiene ítems para liquidar.");
			}
		}

		private async Task InsertarVoucherAsync(
			SqlConnection con,
			SqlTransaction tx,
			Guid voucherId,
			LiquidacionCompraDisponibleDto compra,
			DateTime fechaEmision,
			string moneda,
			string numero)
		{
			const string sql = """
				INSERT INTO dbo.Voucher
				(
					Id,
					CompraId,
					SunatTypeCode,
					Series,
					Number,
					IssuerRuc,
					IssuerLegalName,
					IssueDate,
					Currency,
					Subtotal,
					Igv,
					Total,
					SunatStatus
				)
				VALUES
				(
					@Id,
					@CompraId,
					@SunatTypeCode,
					@Series,
					@Number,
					@IssuerRuc,
					@IssuerLegalName,
					@IssueDate,
					@Currency,
					@Subtotal,
					@Igv,
					@Total,
					'NO_ENVIADO'
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.AddWithValue("@CompraId", compra.CompraId);
			cmd.Parameters.AddWithValue("@SunatTypeCode", LiquidacionCompraUblBuilder.TipoDocumentoLiquidacionCompra);
			cmd.Parameters.AddWithValue("@Series", SerieLiquidacion);
			cmd.Parameters.AddWithValue("@Number", numero);
			cmd.Parameters.AddWithValue("@IssuerRuc", _settings.Emisor.Ruc);
			cmd.Parameters.AddWithValue("@IssuerLegalName", _settings.Emisor.RazonSocial);
			cmd.Parameters.AddWithValue("@IssueDate", fechaEmision);
			cmd.Parameters.AddWithValue("@Currency", moneda);
			cmd.Parameters.AddWithValue("@Subtotal", compra.Subtotal);
			cmd.Parameters.AddWithValue("@Igv", compra.Igv);
			cmd.Parameters.AddWithValue("@Total", compra.Total);
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task InsertarItemsAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, List<LiquidacionCompraDetalleDto> items)
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

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@LineNumber", i + 1);
				cmd.Parameters.AddWithValue("@ProductId", item.ProductoId);
				cmd.Parameters.AddWithValue("@ProductCode", string.IsNullOrWhiteSpace(item.Codigo) ? DBNull.Value : item.Codigo);
				cmd.Parameters.AddWithValue("@Description", item.Descripcion);
				cmd.Parameters.AddWithValue("@Quantity", item.Cantidad);
				cmd.Parameters.AddWithValue("@UnitCode", item.UnidadMedida);
				cmd.Parameters.AddWithValue("@UnitPrice", item.PrecioUnitario);
				cmd.Parameters.AddWithValue("@SaleValue", item.ValorVenta);
				cmd.Parameters.AddWithValue("@IgvPercentage", 18m);
				cmd.Parameters.AddWithValue("@Igv", item.Igv);
				cmd.Parameters.AddWithValue("@Total", item.Importe);
				await cmd.ExecuteNonQueryAsync();
			}
		}
	}
}
