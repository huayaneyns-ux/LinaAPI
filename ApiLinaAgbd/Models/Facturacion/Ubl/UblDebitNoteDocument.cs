using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	/// <summary>
	/// Cuerpo UBL Debit Note (tipo 08).
	/// </summary>
	public class UblDebitNoteDocument
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

		[JsonPropertyName("cbc:InvoiceTypeCode")]
		public UblNode InvoiceTypeCode { get; set; } = UblNode.Value("08");

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

		[JsonPropertyName("cac:RequestedMonetaryTotal")]
		public UblRequestedMonetaryTotal RequestedMonetaryTotal { get; set; } = new();

		[JsonPropertyName("cac:DebitNoteLine")]
		public List<UblDebitNoteLine> DebitNoteLine { get; set; } = new();
	}

	public class UblDiscrepancyResponse
	{
		[JsonPropertyName("cbc:ReferenceID")]
		public UblNode ReferenceId { get; set; } = new();

		[JsonPropertyName("cbc:ResponseCode")]
		public UblNode ResponseCode { get; set; } = new();

		[JsonPropertyName("cbc:Description")]
		public UblNode Description { get; set; } = new();
	}

	public class UblBillingReference
	{
		[JsonPropertyName("cac:InvoiceDocumentReference")]
		public UblInvoiceDocumentReference InvoiceDocumentReference { get; set; } = new();
	}

	public class UblInvoiceDocumentReference
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = new();

		[JsonPropertyName("cbc:DocumentTypeCode")]
		public UblNode DocumentTypeCode { get; set; } = new();
	}

	public class UblRequestedMonetaryTotal
	{
		[JsonPropertyName("cbc:PayableAmount")]
		public UblNode PayableAmount { get; set; } = new();
	}

	public class UblDebitNoteLine
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = new();

		[JsonPropertyName("cbc:DebitedQuantity")]
		public UblNode DebitedQuantity { get; set; } = new();

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
