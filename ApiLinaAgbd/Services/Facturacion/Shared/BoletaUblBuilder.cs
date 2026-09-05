using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion.Shared
{
	public class BoletaUblBuilder
	{
		public const string TipoDocumentoBoleta = "03";
		public const string CodigoOperacionVentaInterna = "0101";

		private readonly EmisorSettings _emisor;

		public BoletaUblBuilder(IOptions<FacturacionSettings> options)
		{
			_emisor = options.Value.Emisor ?? new EmisorSettings();
		}

		public UblInvoiceDocument Build(UblInvoicePayloadDto request)
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
				UblVersionId = UblNode.Value("2.1"),
				CustomizationId = UblNode.Value("2.0"),
				Id = UblNode.Value($"{request.Serie}-{request.Correlativo}"),
				IssueDate = UblNode.Value(request.FechaEmision),
				IssueTime = UblNode.Value(horaEmision),
				InvoiceTypeCode = UblNode.Attr(TipoDocumentoBoleta, "listID", CodigoOperacionVentaInterna),
				Note =
				[
					UblNode.Attr(montoEnLetras, "languageLocaleID", "1000")
				],
				DocumentCurrencyCode = UblNode.Value(moneda),
				AccountingSupplierParty = BuildEmisor(),
				AccountingCustomerParty = BuildCliente(request.Cliente),
				TaxTotal = BuildTaxTotal(request.Totales.ValorVenta, request.Totales.Igv, moneda),
				LegalMonetaryTotal = new UblLegalMonetaryTotal
				{
					LineExtensionAmount = UblNode.Amount(request.Totales.ValorVenta, moneda),
					TaxInclusiveAmount = UblNode.Amount(request.Totales.Total, moneda),
					PayableAmount = UblNode.Amount(request.Totales.Total, moneda)
				},
				InvoiceLine = BuildLineas(request.Items, moneda)
			};
		}

		private UblAccountingParty BuildEmisor()
		{
			return new UblAccountingParty
			{
				Party = new UblParty
				{
					PartyIdentification = new UblPartyIdentification
					{
						Id = UblNode.Attr(_emisor.Ruc, "schemeID", _emisor.TipoDocumento)
					},
					PartyName = new UblPartyName
					{
						Name = UblNode.Value(_emisor.NombreComercial)
					},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(_emisor.RazonSocial),
						RegistrationAddress = new UblRegistrationAddress
						{
							AddressTypeCode = UblNode.Value(_emisor.CodigoEstablecimiento),
							AddressLine = new UblAddressLine
							{
								Line = UblNode.Value(_emisor.Direccion)
							}
						}
					}
				}
			};
		}

		private static UblAccountingParty BuildCliente(UblPartyPayloadDto cliente)
		{
			var direccion = string.IsNullOrWhiteSpace(cliente.Direccion)
				? null
				: new UblRegistrationAddress
				{
					AddressLine = new UblAddressLine
					{
						Line = UblNode.Value(cliente.Direccion)
					}
				};

			return new UblAccountingParty
			{
				Party = new UblParty
				{
					PartyIdentification = new UblPartyIdentification
					{
						Id = UblNode.Attr(cliente.NumeroDocumento, "schemeID", cliente.TipoDocumento)
					},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(cliente.Nombre),
						RegistrationAddress = direccion
					}
				}
			};
		}

		private static UblTaxTotal BuildTaxTotal(decimal baseImponible, decimal igv, string moneda, decimal? porcentaje = null, string? codigoAfectacion = null)
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
							Percent = porcentaje.HasValue ? UblNode.Value(porcentaje.Value) : null,
							TaxExemptionReasonCode = string.IsNullOrWhiteSpace(codigoAfectacion)
								? null
								: UblNode.Value(codigoAfectacion),
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
				var unidad = string.IsNullOrWhiteSpace(item.UnidadMedida) ? "NIU" : item.UnidadMedida;

				lineas.Add(new UblInvoiceLine
				{
					Id = UblNode.Value(i + 1),
					InvoicedQuantity = UblNode.Attr(item.Cantidad, "unitCode", unidad),
					LineExtensionAmount = UblNode.Amount(item.ValorVenta, moneda),
					PricingReference = new UblPricingReference
					{
						AlternativeConditionPrice = new UblAlternativeConditionPrice
						{
							PriceAmount = UblNode.Amount(item.PrecioConIgv, moneda),
							PriceTypeCode = UblNode.Value("01")
						}
					},
					TaxTotal = BuildTaxTotal(
						item.ValorVenta,
						item.Igv,
						moneda,
						item.PorcentajeIgv,
						item.CodigoAfectacionIgv),
					Item = new UblItem
					{
						Description = UblNode.Value(item.Descripcion)
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
