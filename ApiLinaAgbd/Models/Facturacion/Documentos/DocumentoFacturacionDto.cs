namespace ApiLinaAgbd.Models.Facturacion.Documentos
{
	public class DocumentoFacturacionDto
	{
		public string Id { get; set; } = string.Empty;

		public string Tipo { get; set; } = string.Empty;

		public string Serie { get; set; } = string.Empty;

		public string Numero { get; set; } = string.Empty;

		public string FechaEmision { get; set; } = string.Empty;

		public string Moneda { get; set; } = "PEN";

		public string Estado { get; set; } = string.Empty;

		public string EstadoSunat { get; set; } = string.Empty;

		public string? DocumentId { get; set; }

		public string CodigoRespuestaSunat { get; set; } = string.Empty;

		public string MensajeSunat { get; set; } = string.Empty;

		public string DetalleError { get; set; } = string.Empty;

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }

		public string? VentaOrigenId { get; set; }

		public string? CompraOrigenId { get; set; }

		public string? VoucherReferenciaId { get; set; }

		public string? DocumentoReferencia { get; set; }
	}
}
