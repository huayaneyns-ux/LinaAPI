using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	public class UblCreditNoteDocument
	{
		[JsonPropertyName("cbc:UBLVersionID")]
		public UblNode UblVersionId { get; set; } = UblNode.Value("2.1");

		[JsonPropertyName("cbc:CustomizationID")]
		public UblNode CustomizationId { get; set; } = UblNode.Value("2.0");

		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = UblNode.Value(string.Empty);

		[JsonPropertyName("cbc:IssueDate")]
		public UblNode IssueDate { get; set; } = UblNode.Value(string.Empty);

		[JsonPropertyName("cbc:IssueTime")]
		public UblNode IssueTime { get; set; } = UblNode.Value(string.Empty);

		[JsonPropertyName("cbc:Note")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<UblNode>? Note { get; set; }

		[JsonPropertyName("cbc:DocumentCurrencyCode")]
		public UblNode DocumentCurrencyCode { get; set; } = UblNode.Value("PEN");

		[JsonPropertyName("cac:DiscrepancyResponse")]
		public UblDiscrepancyResponse DiscrepancyResponse { get; set; } = new();

		[JsonPropertyName("cac:BillingReference")]
		public UblBillingReference BillingReference { get; set; } = new();

		[JsonPropertyName("cac:AccountingSupplierParty")]
		public UblAccountingParty AccountingSupplierParty { get; set; } = new();

		[JsonPropertyName("cac:AccountingCustomerParty")]
		public UblAccountingParty AccountingCustomerParty { get; set; } = new();

		[JsonPropertyName("cac:TaxTotal")]
		public UblTaxTotal TaxTotal { get; set; } = new();

		[JsonPropertyName("cac:LegalMonetaryTotal")]
		public UblLegalMonetaryTotal LegalMonetaryTotal { get; set; } = new();

		[JsonPropertyName("cac:CreditNoteLine")]
		public List<UblCreditNoteLine> CreditNoteLine { get; set; } = new();
	}

	public class UblCreditNoteLine
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = new();

		[JsonPropertyName("cbc:CreditedQuantity")]
		public UblNode CreditedQuantity { get; set; } = new();

		[JsonPropertyName("cbc:LineExtensionAmount")]
		public UblNode LineExtensionAmount { get; set; } = new();

		[JsonPropertyName("cac:PricingReference")]
		public UblPricingReference PricingReference { get; set; } = new();

		[JsonPropertyName("cac:TaxTotal")]
		public UblTaxTotal TaxTotal { get; set; } = new();

		[JsonPropertyName("cac:Item")]
		public UblItem Item { get; set; } = new();

		[JsonPropertyName("cac:Price")]
		public UblPrice Price { get; set; } = new();
	}
}
