using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.Boleta
{
	public class BoletaRequestDto
	{
		[Required]
		public string Serie { get; set; } = string.Empty;

		[Required]
		public string Correlativo { get; set; } = string.Empty;

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? HoraEmision { get; set; }

		public string Moneda { get; set; } = "PEN";

		public string? MontoEnLetras { get; set; }

		[Required]
		public BoletaClienteDto Cliente { get; set; } = new();

		[Required]
		public BoletaTotalesDto Totales { get; set; } = new();

		[Required]
		[MinLength(1)]
		public List<BoletaItemDto> Items { get; set; } = new();
	}

	public class BoletaClienteDto
	{
		[Required]
		public string TipoDocumento { get; set; } = string.Empty;

		[Required]
		public string NumeroDocumento { get; set; } = string.Empty;

		[Required]
		public string Nombre { get; set; } = string.Empty;

		public string? Direccion { get; set; }
	}

	public class BoletaTotalesDto
	{
		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }
	}

	public class BoletaItemDto
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
}
