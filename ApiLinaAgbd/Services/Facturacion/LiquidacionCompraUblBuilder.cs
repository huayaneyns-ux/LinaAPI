using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class LiquidacionCompraUblBuilder
	{
		public const string TipoDocumentoLiquidacionCompra = "04";
		private const string CodigoOperacionCompra = "0501";

		private readonly EmisorSettings _emisor;

		public LiquidacionCompraUblBuilder(IOptions<FacturacionSettings> options)
		{
			_emisor = options.Value.Emisor ?? new EmisorSettings();
		}

		public UblInvoiceDocument Build(
			UblInvoicePayloadDto request,
			LiquidacionCompraUbicacionDto puntoVenta)
		{
			var moneda = string.IsNullOrWhiteSpace(request.Moneda) ? "PEN" : request.Moneda;
			var horaEmision = string.IsNullOrWhiteSpace(request.HoraEmision)
				? DateTime.Now.ToString("HH:mm:ss")
				: request.HoraEmision;
			var montoEnLetras = string.IsNullOrWhiteSpace(request.MontoEnLetras)
				? MontoEnLetras.EnSoles(request.Totales.Total)
				: request.MontoEnLetras;

			return new UblInvoiceDocument
			{
				Id = UblNode.Value($"{request.Serie}-{request.Correlativo}"),
				IssueDate = UblNode.Value(request.FechaEmision),
				IssueTime = UblNode.Value(horaEmision),
				InvoiceTypeCode = UblNode.Attr(TipoDocumentoLiquidacionCompra, "listID", CodigoOperacionCompra),
				Note =
				[
					UblNode.Attr(montoEnLetras, "languageLocaleID", "1000")
				],
				DocumentCurrencyCode = UblNode.Value(moneda),
				AccountingSupplierParty = BuildProveedor(request.Cliente),
				AccountingCustomerParty = BuildAdquirente(puntoVenta),
				TaxTotal = BuildTaxTotal(request.Totales.ValorVenta, request.Totales.Igv, moneda),
				LegalMonetaryTotal = new UblLegalMonetaryTotal
				{
					LineExtensionAmount = UblNode.Amount(request.Totales.ValorVenta, moneda),
					TaxInclusiveAmount = UblNode.Amount(request.Totales.Total, moneda),
					PayableAmount = UblNode.Amount(request.Totales.ValorVenta, moneda)
				},
				DeliveryTerms = new UblDeliveryTerms
				{
					DeliveryLocation = new UblDeliveryLocation
					{
						LocationTypeCode = UblNode.Value("01"),
						Address = BuildDeliveryAddress(puntoVenta)
					}
				},
				InvoiceLine = BuildLineas(request.Items, moneda)
			};
		}

		private static UblAccountingParty BuildProveedor(UblPartyPayloadDto vendedor)
		{
			return new UblAccountingParty
			{
				Party = new UblParty
				{
					PartyIdentification = new UblPartyIdentification
					{
						Id = UblNode.Attr(vendedor.NumeroDocumento, "schemeID", vendedor.TipoDocumento)
					},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(vendedor.Nombre),
						RegistrationAddress = string.IsNullOrWhiteSpace(vendedor.Direccion)
							? null
							: BuildSupplierRegistrationAddress(vendedor)
					}
				}
			};
		}

		private UblAccountingParty BuildAdquirente(LiquidacionCompraUbicacionDto puntoVenta)
		{
			return new UblAccountingParty
			{
				Party = new UblParty
				{
					PartyIdentification = new UblPartyIdentification
					{
						Id = UblNode.Attr(_emisor.Ruc, "schemeID", _emisor.TipoDocumento)
					},
					PartyName = string.IsNullOrWhiteSpace(_emisor.NombreComercial)
						? new UblPartyName
						{
							Name = UblNode.Value(_emisor.RazonSocial)
						}
						: new UblPartyName
						{
							Name = UblNode.Value(_emisor.NombreComercial)
						},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(_emisor.RazonSocial),
						RegistrationAddress = string.IsNullOrWhiteSpace(_emisor.Direccion)
							? null
							: new UblRegistrationAddress
							{
								AddressLine = new UblAddressLine
								{
									Line = UblNode.Value(_emisor.Direccion)
								}
							}
					}
				}
			};
		}

		private static UblRegistrationAddress BuildRegistrationAddress(
			LiquidacionCompraUbicacionDto ubicacion,
			bool incluirCodigoEstablecimiento)
		{
			return new UblRegistrationAddress
			{
				Id = string.IsNullOrWhiteSpace(ubicacion.CodigoUbigeo)
					? null
					: UblNode.Value(ubicacion.CodigoUbigeo),
				CityName = string.IsNullOrWhiteSpace(ubicacion.Provincia)
					? null
					: UblNode.Value(ubicacion.Provincia),
				CountrySubentity = string.IsNullOrWhiteSpace(ubicacion.Departamento)
					? null
					: UblNode.Value(ubicacion.Departamento),
				District = string.IsNullOrWhiteSpace(ubicacion.Distrito)
					? null
					: UblNode.Value(ubicacion.Distrito),
				AddressTypeCode = incluirCodigoEstablecimiento && !string.IsNullOrWhiteSpace(ubicacion.CodigoEstablecimiento)
					? UblNode.Value(ubicacion.CodigoEstablecimiento)
					: null,
				AddressLine = string.IsNullOrWhiteSpace(ubicacion.Direccion)
					? null
					: new UblAddressLine
					{
						Line = UblNode.Value(ubicacion.Direccion)
					}
			};
		}

		private static UblRegistrationAddress BuildDeliveryAddress(LiquidacionCompraUbicacionDto ubicacion)
		{
			return new UblRegistrationAddress
			{
				Id = string.IsNullOrWhiteSpace(ubicacion.CodigoUbigeo)
					? null
					: UblNode.Value(ubicacion.CodigoUbigeo),
				AddressLine = string.IsNullOrWhiteSpace(ubicacion.Direccion)
					? null
					: new UblAddressLine
					{
						Line = UblNode.Value(ubicacion.Direccion)
					}
			};
		}

		private static UblRegistrationAddress BuildSupplierRegistrationAddress(UblPartyPayloadDto vendedor)
		{
			return new UblRegistrationAddress
			{
				Id = string.IsNullOrWhiteSpace(vendedor.CodigoUbigeo)
					? null
					: UblNode.Value(vendedor.CodigoUbigeo),
				AddressTypeCode = UblNode.Value("05"),
				AddressLine = string.IsNullOrWhiteSpace(vendedor.Direccion)
					? null
					: new UblAddressLine
					{
						Line = UblNode.Value(vendedor.Direccion)
					}
			};
		}

		private static UblTaxTotal BuildTaxTotal(decimal baseImponible, decimal igv, string moneda)
		{
			return new UblTaxTotal
			{
				TaxAmount = UblNode.Amount(igv, moneda),
				TaxSubtotal =
				[
					new UblTaxSubtotal
					{
						TaxableAmount = UblNode.Amount(baseImponible, moneda),
						TaxAmount = UblNode.Amount(igv, moneda),
						TaxCategory = new UblTaxCategory
						{
							Percent = UblNode.Value(18),
							TaxExemptionReasonCode = UblNode.Value("10"),
							TaxScheme = new UblTaxScheme()
						}
					}
				]
			};
		}

		private static List<UblInvoiceLine> BuildLineas(List<UblItemPayloadDto> items, string moneda)
		{
			var lineas = new List<UblInvoiceLine>();

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				lineas.Add(new UblInvoiceLine
				{
					Id = UblNode.Value(i + 1),
					InvoicedQuantity = UblNode.Attr(item.Cantidad, "unitCode", string.IsNullOrWhiteSpace(item.UnidadMedida) ? "NIU" : item.UnidadMedida),
					LineExtensionAmount = UblNode.Amount(item.ValorVenta, moneda),
					PricingReference = new UblPricingReference
					{
						AlternativeConditionPrice = new UblAlternativeConditionPrice
						{
							PriceAmount = UblNode.Amount(item.PrecioConIgv, moneda),
							PriceTypeCode = UblNode.Value("01")
						}
					},
					TaxTotal = BuildTaxTotal(item.ValorVenta, item.Igv, moneda),
					Item = new UblItem
					{
						Description = UblNode.Value(item.Descripcion),
						SellersItemIdentification = new UblSellersItemIdentification
						{
							Id = UblNode.Value(string.IsNullOrWhiteSpace(item.Codigo) ? (i + 1).ToString() : item.Codigo)
						}
					},
					Price = new UblPrice
					{
						PriceAmount = UblNode.Amount(item.PrecioUnitario, moneda)
					}
				});
			}

			return lineas;
		}
	}
}
