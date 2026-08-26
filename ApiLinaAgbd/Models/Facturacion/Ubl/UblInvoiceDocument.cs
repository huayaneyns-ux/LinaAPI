using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	/// <summary>
	/// Cuerpo UBL Invoice (boleta y factura usan el mismo documento, distinto InvoiceTypeCode).
	/// </summary>
	public class UblInvoiceDocument
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
		public UblNode InvoiceTypeCode { get; set; } = new();

		[JsonPropertyName("cbc:Note")]
		public List<UblNode> Note { get; set; } = new();

		[JsonPropertyName("cbc:DocumentCurrencyCode")]
		public UblNode DocumentCurrencyCode { get; set; } = UblNode.Value("PEN");

		[JsonPropertyName("cac:AccountingSupplierParty")]
		public UblAccountingParty AccountingSupplierParty { get; set; } = new();

		[JsonPropertyName("cac:AccountingCustomerParty")]
		public UblAccountingParty AccountingCustomerParty { get; set; } = new();

		[JsonPropertyName("cac:TaxTotal")]
		public UblTaxTotal TaxTotal { get; set; } = new();

		[JsonPropertyName("cac:LegalMonetaryTotal")]
		public UblLegalMonetaryTotal LegalMonetaryTotal { get; set; } = new();

		[JsonPropertyName("cac:InvoiceLine")]
		public List<UblInvoiceLine> InvoiceLine { get; set; } = new();
	}

	public class UblAccountingParty
	{
		[JsonPropertyName("cac:Party")]
		public UblParty Party { get; set; } = new();
	}

	public class UblParty
	{
		[JsonPropertyName("cac:PartyIdentification")]
		public UblPartyIdentification PartyIdentification { get; set; } = new();

		[JsonPropertyName("cac:PartyName")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblPartyName? PartyName { get; set; }

		[JsonPropertyName("cac:PartyLegalEntity")]
		public UblPartyLegalEntity PartyLegalEntity { get; set; } = new();
	}

	public class UblPartyIdentification
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = new();
	}

	public class UblPartyName
	{
		[JsonPropertyName("cbc:Name")]
		public UblNode Name { get; set; } = new();
	}

	public class UblPartyLegalEntity
	{
		[JsonPropertyName("cbc:RegistrationName")]
		public UblNode RegistrationName { get; set; } = new();

		[JsonPropertyName("cac:RegistrationAddress")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblRegistrationAddress? RegistrationAddress { get; set; }
	}

	public class UblRegistrationAddress
	{
		[JsonPropertyName("cbc:AddressTypeCode")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblNode? AddressTypeCode { get; set; }

		[JsonPropertyName("cac:AddressLine")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblAddressLine? AddressLine { get; set; }
	}

	public class UblAddressLine
	{
		[JsonPropertyName("cbc:Line")]
		public UblNode Line { get; set; } = new();
	}

	public class UblTaxTotal
	{
		[JsonPropertyName("cbc:TaxAmount")]
		public UblNode TaxAmount { get; set; } = new();

		[JsonPropertyName("cac:TaxSubtotal")]
		public List<UblTaxSubtotal> TaxSubtotal { get; set; } = new();
	}

	public class UblTaxSubtotal
	{
		[JsonPropertyName("cbc:TaxableAmount")]
		public UblNode TaxableAmount { get; set; } = new();

		[JsonPropertyName("cbc:TaxAmount")]
		public UblNode TaxAmount { get; set; } = new();

		[JsonPropertyName("cac:TaxCategory")]
		public UblTaxCategory TaxCategory { get; set; } = new();
	}

	public class UblTaxCategory
	{
		[JsonPropertyName("cbc:Percent")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblNode? Percent { get; set; }

		[JsonPropertyName("cbc:TaxExemptionReasonCode")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public UblNode? TaxExemptionReasonCode { get; set; }

		[JsonPropertyName("cac:TaxScheme")]
		public UblTaxScheme TaxScheme { get; set; } = new();
	}

	public class UblTaxScheme
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = UblNode.Value("1000");

		[JsonPropertyName("cbc:Name")]
		public UblNode Name { get; set; } = UblNode.Value("IGV");

		[JsonPropertyName("cbc:TaxTypeCode")]
		public UblNode TaxTypeCode { get; set; } = UblNode.Value("VAT");
	}

	public class UblLegalMonetaryTotal
	{
		[JsonPropertyName("cbc:LineExtensionAmount")]
		public UblNode LineExtensionAmount { get; set; } = new();

		[JsonPropertyName("cbc:TaxInclusiveAmount")]
		public UblNode TaxInclusiveAmount { get; set; } = new();

		[JsonPropertyName("cbc:PayableAmount")]
		public UblNode PayableAmount { get; set; } = new();
	}

	public class UblInvoiceLine
	{
		[JsonPropertyName("cbc:ID")]
		public UblNode Id { get; set; } = new();

		[JsonPropertyName("cbc:InvoicedQuantity")]
		public UblNode InvoicedQuantity { get; set; } = new();

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

	public class UblPricingReference
	{
		[JsonPropertyName("cac:AlternativeConditionPrice")]
		public UblAlternativeConditionPrice AlternativeConditionPrice { get; set; } = new();
	}

	public class UblAlternativeConditionPrice
	{
		[JsonPropertyName("cbc:PriceAmount")]
		public UblNode PriceAmount { get; set; } = new();

		[JsonPropertyName("cbc:PriceTypeCode")]
		public UblNode PriceTypeCode { get; set; } = UblNode.Value("01");
	}

	public class UblItem
	{
		[JsonPropertyName("cbc:Description")]
		public UblNode Description { get; set; } = new();
	}

	public class UblPrice
	{
		[JsonPropertyName("cbc:PriceAmount")]
		public UblNode PriceAmount { get; set; } = new();
	}
}
