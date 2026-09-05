using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.LiquidacionCompra
{
	public class LiquidacionCompraEmitirRequestDto
	{
		[Range(1, int.MaxValue)]
		public int CompraOrigenId { get; set; }

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? HoraEmision { get; set; }

		public string Moneda { get; set; } = "PEN";

		public string? Observaciones { get; set; }

		[Required]
		public LiquidacionCompraVendedorDto Vendedor { get; set; } = new();

		[Required]
		public LiquidacionCompraUbicacionDto UbicacionVendedor { get; set; } = new();

		[Required]
		public LiquidacionCompraUbicacionDto PuntoVenta { get; set; } = new();
	}

	public class LiquidacionCompraUbicacionDto
	{
		public int DistritoId { get; set; }

		[Required]
		public string Direccion { get; set; } = string.Empty;

		public string? CodigoEstablecimiento { get; set; }

		public string? CodigoUbigeo { get; set; }

		public string? Departamento { get; set; }

		public string? Provincia { get; set; }

		public string? Distrito { get; set; }
	}

	public class LiquidacionCompraDisponibleDto
	{
		public int CompraId { get; set; }

		public string Codigo { get; set; } = string.Empty;

		public string FechaCompra { get; set; } = string.Empty;

		public LiquidacionCompraVendedorDto Vendedor { get; set; } = new();

		public LiquidacionCompraUbicacionDisponibleDto? UbicacionVendedor { get; set; }

		public List<LiquidacionCompraDetalleDto> Detalle { get; set; } = new();

		public decimal Subtotal { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }
	}

	public class LiquidacionCompraVendedorDto
	{
		public string TipoDocumento { get; set; } = string.Empty;

		public string NumeroDocumento { get; set; } = string.Empty;

		public string Nombre { get; set; } = string.Empty;

		public string? NombreContacto { get; set; }
	}

	public class LiquidacionCompraUbicacionDisponibleDto
	{
		public int DistritoId { get; set; }

		public string Departamento { get; set; } = string.Empty;

		public string Provincia { get; set; } = string.Empty;

		public string Distrito { get; set; } = string.Empty;

		public string Direccion { get; set; } = string.Empty;
	}

	public class LiquidacionCompraDetalleDto
	{
		public int ProductoId { get; set; }

		public string Codigo { get; set; } = string.Empty;

		public string Descripcion { get; set; } = string.Empty;

		public decimal Cantidad { get; set; }

		public decimal PrecioUnitario { get; set; }

		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal Importe { get; set; }

		public string UnidadMedida { get; set; } = "NIU";
	}
}
