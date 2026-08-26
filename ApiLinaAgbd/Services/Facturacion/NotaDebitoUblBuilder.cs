using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class NotaDebitoUblBuilder
	{
		public const string TipoDocumentoNotaDebito = "08";

		private readonly EmisorSettings _emisor;

		public NotaDebitoUblBuilder(IOptions<FacturacionSettings> options)
		{
			_emisor = options.Value.Emisor ?? new EmisorSettings();
		}

		public UblDebitNoteDocument Build(NotaDebitoRequestDto request)
		{
			var moneda = string.IsNullOrWhiteSpace(request.Moneda) ? "PEN" : request.Moneda;
			var horaEmision = string.IsNullOrWhiteSpace(request.HoraEmision)
				? DateTime.Now.ToString("HH:mm:ss")
				: request.HoraEmision;

			return new UblDebitNoteDocument
			{
				UblVersionId = UblNode.Value("2.1"),
				CustomizationId = UblNode.Value("2.0"),
				Id = UblNode.Value($"{request.Serie}-{request.Correlativo}"),
				IssueDate = UblNode.Value(request.FechaEmision),
				IssueTime = UblNode.Value(horaEmision),
				InvoiceTypeCode = UblNode.Value(TipoDocumentoNotaDebito),
				DocumentCurrencyCode = UblNode.Value(moneda),
				DiscrepancyResponse = new UblDiscrepancyResponse
				{
					ReferenceId = UblNode.Value(request.DocumentoReferencia.Id),
					ResponseCode = UblNode.Value(request.Motivo.Codigo),
					Description = UblNode.Value(request.Motivo.Descripcion)
				},
				BillingReference = new UblBillingReference
				{
					InvoiceDocumentReference = new UblInvoiceDocumentReference
					{
						Id = UblNode.Value(request.DocumentoReferencia.Id),
						DocumentTypeCode = UblNode.Value(request.DocumentoReferencia.TipoDocumento)
					}
				},
				AccountingSupplierParty = BuildEmisor(),
				AccountingCustomerParty = BuildCliente(request.Cliente),
				TaxTotal = BuildTaxTotal(request.Totales.ValorVenta, request.Totales.Igv, moneda),
				RequestedMonetaryTotal = new UblRequestedMonetaryTotal
				{
					PayableAmount = UblNode.Amount(request.Totales.Total, moneda)
				},
				DebitNoteLine = BuildLineas(request.Items, moneda)
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
						RegistrationName = UblNode.Value(_emisor.RazonSocial)
					}
				}
			};
		}

		private static UblAccountingParty BuildCliente(NotaDebitoClienteDto cliente)
		{
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
						RegistrationName = UblNode.Value(cliente.Nombre)
					}
				}
			};
		}

		private static UblTaxTotal BuildTaxTotal(
			decimal baseImponible,
			decimal igv,
			string moneda,
			decimal? porcentaje = null,
			string? codigoAfectacion = null)
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

		private static List<UblDebitNoteLine> BuildLineas(List<NotaDebitoItemDto> items, string moneda)
		{
			var lineas = new List<UblDebitNoteLine>();

			for (var i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var unidad = string.IsNullOrWhiteSpace(item.UnidadMedida) ? "NIU" : item.UnidadMedida;

				lineas.Add(new UblDebitNoteLine
				{
					Id = UblNode.Value(i + 1),
					DebitedQuantity = UblNode.Attr(item.Cantidad, "unitCode", unidad),
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
