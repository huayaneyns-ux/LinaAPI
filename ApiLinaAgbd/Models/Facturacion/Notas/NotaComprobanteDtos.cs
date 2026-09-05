namespace ApiLinaAgbd.Models.Facturacion.Notas
{
	public class NotaComprobanteBaseDisponibleDto
	{
		public string Id { get; set; } = string.Empty;

		public string Tipo { get; set; } = string.Empty;

		public string SunatTypeCode { get; set; } = string.Empty;

		public string Serie { get; set; } = string.Empty;

		public string Numero { get; set; } = string.Empty;

		public string FechaEmision { get; set; } = string.Empty;

		public string Moneda { get; set; } = "PEN";

		public string ClienteNombre { get; set; } = string.Empty;

		public string ClienteTipoDocumento { get; set; } = string.Empty;

		public string ClienteDocumento { get; set; } = string.Empty;

		public string ClienteDireccion { get; set; } = string.Empty;

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }

		public List<NotaComprobanteBaseItemDto> Items { get; set; } = new();
	}

	public class NotaComprobanteBaseItemDto
	{
		public string Id { get; set; } = string.Empty;

		public int? ProductoId { get; set; }

		public string Codigo { get; set; } = string.Empty;

		public string Descripcion { get; set; } = string.Empty;

		public decimal Cantidad { get; set; }

		public decimal PrecioUnitario { get; set; }

		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal Importe { get; set; }

		public string UnidadMedida { get; set; } = "NIU";
	}

	public class NotaComprobanteResultadoDto
	{
		public string Id { get; set; } = string.Empty;

		public string Tipo { get; set; } = string.Empty;

		public string Serie { get; set; } = string.Empty;

		public string Numero { get; set; } = string.Empty;

		public string FechaEmision { get; set; } = string.Empty;

		public string Moneda { get; set; } = "PEN";

		public string EstadoSunat { get; set; } = string.Empty;

		public string? DocumentId { get; set; }

		public string CodigoRespuestaSunat { get; set; } = string.Empty;

		public string MensajeSunat { get; set; } = string.Empty;

		public string DetalleError { get; set; } = string.Empty;

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }

		public string VoucherReferenciaId { get; set; } = string.Empty;

		public string DocumentoReferencia { get; set; } = string.Empty;
	}
}
