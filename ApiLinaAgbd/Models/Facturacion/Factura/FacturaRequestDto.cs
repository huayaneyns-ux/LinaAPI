using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.Factura
{
	public class FacturaRequestDto
	{
		[Required]
		public string Serie { get; set; } = string.Empty;

		[Required]
		public string Correlativo { get; set; } = string.Empty;

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? FechaVencimiento { get; set; }

		public string? HoraEmision { get; set; }

		public string Moneda { get; set; } = "PEN";

		/// <summary>
		/// Opcional. Si no se envía, el backend lo genera a partir de totales.total (no recalcula importes).
		/// </summary>
		public string? MontoEnLetras { get; set; }

		[Required]
		public FacturaClienteDto Cliente { get; set; } = new();

		[Required]
		public FacturaTotalesDto Totales { get; set; } = new();

		[Required]
		[MinLength(1)]
		public List<FacturaItemDto> Items { get; set; } = new();

		public FacturaPagoDto? Pago { get; set; }
	}

	public class FacturaClienteDto
	{
		/// <summary>Para factura suele ser "6" (RUC).</summary>
		[Required]
		public string TipoDocumento { get; set; } = "6";

		[Required]
		public string NumeroDocumento { get; set; } = string.Empty;

		[Required]
		public string Nombre { get; set; } = string.Empty;

		public string? Direccion { get; set; }
	}

	public class FacturaTotalesDto
	{
		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }
	}

	public class FacturaItemDto
	{
		[Required]
		public string Descripcion { get; set; } = string.Empty;

		public decimal Cantidad { get; set; }

		public decimal PrecioUnitario { get; set; }

		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal PrecioConIgv { get; set; }

		public string UnidadMedida { get; set; } = "NIU";

		public decimal PorcentajeIgv { get; set; } = 18;

		public string CodigoAfectacionIgv { get; set; } = "10";
	}

	public class FacturaPagoDto
	{
		public string FormaPago { get; set; } = "Contado";

		public List<FacturaCuotaDto> Cuotas { get; set; } = new();
	}

	public class FacturaCuotaDto
	{
		public decimal Monto { get; set; }

		public string FechaVencimiento { get; set; } = string.Empty;
	}
}
