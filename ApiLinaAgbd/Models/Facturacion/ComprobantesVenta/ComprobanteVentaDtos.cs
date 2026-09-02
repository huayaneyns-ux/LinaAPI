using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.ComprobantesVenta
{
	public class ComprobanteVentaEmitirRequestDto
	{
		[Required]
		public string Tipo { get; set; } = string.Empty;

		[Range(1, int.MaxValue)]
		public int VentaOrigenId { get; set; }

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? FechaVencimiento { get; set; }

		public string Moneda { get; set; } = "PEN";

		public string? Observaciones { get; set; }

		[Required]
		public ComprobanteVentaClienteDto Cliente { get; set; } = new();

		public ComprobanteVentaPagoDto? Pago { get; set; }
	}

	public class ComprobanteVentaClienteDto
	{
		public string TipoDocumento { get; set; } = string.Empty;

		public string Documento { get; set; } = string.Empty;

		public string Nombre { get; set; } = string.Empty;

		public string Direccion { get; set; } = string.Empty;

		public string Correo { get; set; } = string.Empty;
	}

	public class ComprobanteVentaPagoDto
	{
		[Required]
		public string FormaPago { get; set; } = string.Empty;

		public List<ComprobanteVentaCuotaDto> Cuotas { get; set; } = new();
	}

	public class ComprobanteVentaCuotaDto
	{
		public int Numero { get; set; }

		[Range(typeof(decimal), "0.01", "999999999.99")]
		public decimal Monto { get; set; }

		[Required]
		public string FechaVencimiento { get; set; } = string.Empty;
	}

	public class VentaComprobanteDisponibleDto
	{
		public string Id { get; set; } = string.Empty;

		public string Codigo { get; set; } = string.Empty;

		public string Fecha { get; set; } = string.Empty;

		public ComprobanteVentaClienteDto Cliente { get; set; } = new();

		public List<VentaComprobanteDetalleDto> Detalle { get; set; } = new();

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }
	}

	public class VentaComprobanteDetalleDto
	{
		public string? ItemId { get; set; }

		public int? ProductoId { get; set; }

		public string Codigo { get; set; } = string.Empty;

		public string ProductoServicio { get; set; } = string.Empty;

		public decimal Cantidad { get; set; }

		public decimal Precio { get; set; }

		public decimal Igv { get; set; }

		public decimal Importe { get; set; }

		public string UnidadMedida { get; set; } = "NIU";
	}

	public class ComprobanteVentaListItemDto
	{
		public string Id { get; set; } = string.Empty;

		public string Tipo { get; set; } = string.Empty;

		public string Serie { get; set; } = string.Empty;

		public string Numero { get; set; } = string.Empty;

		public string FechaEmision { get; set; } = string.Empty;

		public string Cliente { get; set; } = string.Empty;

		public string DocumentoCliente { get; set; } = string.Empty;

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }

		public string Estado { get; set; } = string.Empty;

		public string EstadoSunat { get; set; } = string.Empty;

		public string TipoDocumentoCliente { get; set; } = string.Empty;

		public string DireccionCliente { get; set; } = string.Empty;

		public string CorreoCliente { get; set; } = string.Empty;

		public string CodigoRespuestaSunat { get; set; } = string.Empty;

		public string MensajeSunat { get; set; } = string.Empty;

		public string FechaConsultaSunat { get; set; } = string.Empty;

		public string FechaEnvioSunat { get; set; } = string.Empty;

		public List<VentaComprobanteDetalleDto> Detalle { get; set; } = new();

		public string? DocumentId { get; set; }

		public string? FileName { get; set; }

		public string? PdfUrl { get; set; }

		public string? XmlUrl { get; set; }

		public string? CdrUrl { get; set; }

		public string? VentaOrigenId { get; set; }

		public string? FechaVencimiento { get; set; }

		public string? Observaciones { get; set; }

		public ComprobanteVentaPagoDto? Pago { get; set; }
	}

	public class ComprobanteVentaAnularRequestDto
	{
		[Required]
		[StringLength(100, MinimumLength = 3)]
		public string Reason { get; set; } = string.Empty;
	}
}
