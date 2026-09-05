namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	public class UblInvoicePayloadDto
	{
		public string Serie { get; set; } = string.Empty;
		public string Correlativo { get; set; } = string.Empty;
		public string FechaEmision { get; set; } = string.Empty;
		public string? FechaVencimiento { get; set; }
		public string? HoraEmision { get; set; }
		public string Moneda { get; set; } = "PEN";
		public string? MontoEnLetras { get; set; }
		public UblPartyPayloadDto Cliente { get; set; } = new();
		public UblTotalsPayloadDto Totales { get; set; } = new();
		public List<UblItemPayloadDto> Items { get; set; } = new();
		public UblPaymentPayloadDto? Pago { get; set; }
	}

	public class UblAdjustmentPayloadDto
	{
		public string Serie { get; set; } = string.Empty;
		public string Correlativo { get; set; } = string.Empty;
		public string FechaEmision { get; set; } = string.Empty;
		public string? HoraEmision { get; set; }
		public string Moneda { get; set; } = "PEN";
		public UblReferenceDocumentPayloadDto DocumentoReferencia { get; set; } = new();
		public UblReasonPayloadDto Motivo { get; set; } = new();
		public UblPartyPayloadDto Cliente { get; set; } = new();
		public UblTotalsPayloadDto Totales { get; set; } = new();
		public List<UblItemPayloadDto> Items { get; set; } = new();
	}

	public class UblPartyPayloadDto
	{
		public string TipoDocumento { get; set; } = string.Empty;
		public string NumeroDocumento { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string? Direccion { get; set; }
		public string? CodigoUbigeo { get; set; }
		public string? Departamento { get; set; }
		public string? Provincia { get; set; }
		public string? Distrito { get; set; }
	}

	public class UblTotalsPayloadDto
	{
		public decimal ValorVenta { get; set; }
		public decimal Igv { get; set; }
		public decimal Total { get; set; }
	}

	public class UblItemPayloadDto
	{
		public string? Codigo { get; set; }
		public string Descripcion { get; set; } = string.Empty;
		public decimal Cantidad { get; set; }
		public decimal PrecioUnitario { get; set; }
		public decimal ValorVenta { get; set; }
		public decimal Igv { get; set; }
		public decimal Importe { get; set; }
		public decimal PrecioConIgv { get; set; }
		public string UnidadMedida { get; set; } = "NIU";
		public decimal PorcentajeIgv { get; set; } = 18;
		public string CodigoAfectacionIgv { get; set; } = "10";
	}

	public class UblPaymentPayloadDto
	{
		public string FormaPago { get; set; } = "Contado";
		public List<UblInstallmentPayloadDto> Cuotas { get; set; } = new();
	}

	public class UblInstallmentPayloadDto
	{
		public decimal Monto { get; set; }
		public string FechaVencimiento { get; set; } = string.Empty;
	}

	public class UblReferenceDocumentPayloadDto
	{
		public string Id { get; set; } = string.Empty;
		public string TipoDocumento { get; set; } = string.Empty;
	}

	public class UblReasonPayloadDto
	{
		public string Codigo { get; set; } = string.Empty;
		public string Descripcion { get; set; } = string.Empty;
	}
}
