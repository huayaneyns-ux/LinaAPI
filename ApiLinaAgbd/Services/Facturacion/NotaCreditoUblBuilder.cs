using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.Notas;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;
using static ApiLinaAgbd.Services.Facturacion.MontoEnLetras;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class NotaCreditoUblBuilder
	{
		public const string TipoDocumentoNotaCredito = "07";

		private readonly EmisorSettings _emisor;

		public NotaCreditoUblBuilder(IOptions<FacturacionSettings> options)
		{
			_emisor = options.Value.Emisor ?? new EmisorSettings();
		}

		public UblCreditNoteDocument Build(
			UblAdjustmentPayloadDto request,
			NotaComprobanteBaseDisponibleDto referencia)
		{
			var moneda = string.IsNullOrWhiteSpace(request.Moneda) ? "PEN" : request.Moneda;
			var horaEmision = string.IsNullOrWhiteSpace(request.HoraEmision)
				? DateTime.Now.ToString("HH:mm:ss")
				: request.HoraEmision;
			var items = request.Items;
			var total = request.Totales.Total > 0
				? request.Totales.Total
				: items.Sum(x => x.Importe);
			var valorVenta = request.Totales.ValorVenta > 0
				? request.Totales.ValorVenta
				: items.Sum(x => x.ValorVenta);
			var igv = request.Totales.Igv > 0
				? request.Totales.Igv
				: items.Sum(x => x.Igv);
			var montoEnLetras = EnSoles(total);

			return new UblCreditNoteDocument
			{
				Id = UblNode.Value($"{request.Serie}-{request.Correlativo}"),
				IssueDate = UblNode.Value(request.FechaEmision),
				IssueTime = UblNode.Value(horaEmision),
				Note =
				[
					UblNode.Attr(montoEnLetras, "languageLocaleID", "1000")
				],
				DocumentCurrencyCode = UblNode.Value(moneda),
				DiscrepancyResponse = new UblDiscrepancyResponse
				{
					ResponseCode = UblNode.Value(request.Motivo.Codigo),
					Description = UblNode.Value(request.Motivo.Descripcion)
				},
				BillingReference = new UblBillingReference
				{
					InvoiceDocumentReference = new UblInvoiceDocumentReference
					{
						Id = UblNode.Value($"{referencia.Serie}-{referencia.Numero}"),
						DocumentTypeCode = UblNode.Value(referencia.SunatTypeCode)
					}
				},
				AccountingSupplierParty = BuildEmisor(),
				AccountingCustomerParty = BuildCliente(referencia),
				TaxTotal = BuildTaxTotal(valorVenta, igv, moneda),
				LegalMonetaryTotal = new UblLegalMonetaryTotal
				{
					PayableAmount = UblNode.Amount(total, moneda)
				},
				CreditNoteLine = BuildLineas(items, moneda)
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
					PartyName = string.IsNullOrWhiteSpace(_emisor.NombreComercial)
						? null
						: new UblPartyName
						{
							Name = UblNode.Value(_emisor.NombreComercial)
						},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(_emisor.RazonSocial),
						RegistrationAddress = new UblRegistrationAddress
						{
							AddressTypeCode = string.IsNullOrWhiteSpace(_emisor.CodigoEstablecimiento)
								? null
								: UblNode.Value(_emisor.CodigoEstablecimiento),
							AddressLine = string.IsNullOrWhiteSpace(_emisor.Direccion)
								? null
								: new UblAddressLine
								{
									Line = UblNode.Value(_emisor.Direccion)
								}
						}
					}
				}
			};
		}

		private static UblAccountingParty BuildCliente(NotaComprobanteBaseDisponibleDto referencia)
		{
			var direccion = string.IsNullOrWhiteSpace(referencia.ClienteDireccion)
				? null
				: new UblRegistrationAddress
				{
					AddressLine = new UblAddressLine
					{
						Line = UblNode.Value(referencia.ClienteDireccion)
					}
				};

			return new UblAccountingParty
			{
				Party = new UblParty
				{
					PartyIdentification = new UblPartyIdentification
					{
						Id = UblNode.Attr(
							referencia.ClienteDocumento,
							"schemeID",
							FacturacionVoucherHelper.MapearTipoDocumentoSunat(referencia.ClienteTipoDocumento, referencia.SunatTypeCode == "01"))
					},
					PartyLegalEntity = new UblPartyLegalEntity
					{
						RegistrationName = UblNode.Value(referencia.ClienteNombre),
						RegistrationAddress = direccion
					}
				}
			};
		}

		private static UblTaxTotal BuildTaxTotal(decimal baseImponible, decimal igv, string moneda)
		{
			return new UblTaxTotal
			{
				TaxAmount = UblNode.Amount(baseImponible <= 0 ? 0 : igv, moneda),
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

		private static List<UblCreditNoteLine> BuildLineas(List<UblItemPayloadDto> items, string moneda)
		{
			var lineas = new List<UblCreditNoteLine>();

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				lineas.Add(new UblCreditNoteLine
				{
					Id = UblNode.Value(i + 1),
					CreditedQuantity = UblNode.Attr(item.Cantidad, "unitCode", string.IsNullOrWhiteSpace(item.UnidadMedida) ? "NIU" : item.UnidadMedida),
					LineExtensionAmount = UblNode.Amount(item.ValorVenta, moneda),
					PricingReference = new UblPricingReference
					{
						AlternativeConditionPrice = new UblAlternativeConditionPrice
						{
							PriceAmount = UblNode.Amount(item.PrecioConIgv, moneda),
							PriceTypeCode = UblNode.Value("01")
						}
					},
					TaxTotal = new UblTaxTotal
					{
						TaxAmount = UblNode.Amount(item.Igv, moneda),
						TaxSubtotal =
						[
							new UblTaxSubtotal
							{
								TaxableAmount = UblNode.Amount(item.ValorVenta, moneda),
								TaxAmount = UblNode.Amount(item.Igv, moneda),
								TaxCategory = new UblTaxCategory
								{
									Percent = UblNode.Value(item.PorcentajeIgv),
									TaxExemptionReasonCode = UblNode.Value(item.CodigoAfectacionIgv),
									TaxScheme = new UblTaxScheme()
								}
							}
						]
					},
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
