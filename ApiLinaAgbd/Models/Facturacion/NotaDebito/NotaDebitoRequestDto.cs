using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.NotaDebito
{
	public class NotaDebitoRequestDto
	{
		[Required]
		public string Serie { get; set; } = string.Empty;

		[Required]
		public string Correlativo { get; set; } = string.Empty;

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? HoraEmision { get; set; }

		public string Moneda { get; set; } = "PEN";

		[Required]
		public NotaDebitoDocumentoReferenciaDto DocumentoReferencia { get; set; } = new();

		[Required]
		public NotaDebitoMotivoDto Motivo { get; set; } = new();

		[Required]
		public NotaDebitoClienteDto Cliente { get; set; } = new();

		[Required]
		public NotaDebitoTotalesDto Totales { get; set; } = new();

		[Required]
		[MinLength(1)]
		public List<NotaDebitoItemDto> Items { get; set; } = new();
	}

	public class NotaDebitoDocumentoReferenciaDto
	{
		/// <summary>Ej: F001-00000001 o B001-00000003</summary>
		[Required]
		public string Id { get; set; } = string.Empty;

		/// <summary>01 = Factura, 03 = Boleta</summary>
		[Required]
		public string TipoDocumento { get; set; } = string.Empty;
	}

	public class NotaDebitoMotivoDto
	{
		/// <summary>Código SUNAT del motivo (ResponseCode).</summary>
		[Required]
		public string Codigo { get; set; } = string.Empty;

		[Required]
		public string Descripcion { get; set; } = string.Empty;
	}

	public class NotaDebitoClienteDto
	{
		[Required]
		public string TipoDocumento { get; set; } = string.Empty;

		[Required]
		public string NumeroDocumento { get; set; } = string.Empty;

		[Required]
		public string Nombre { get; set; } = string.Empty;
	}

	public class NotaDebitoTotalesDto
	{
		public decimal ValorVenta { get; set; }

		public decimal Igv { get; set; }

		public decimal Total { get; set; }
	}

	public class NotaDebitoItemDto
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
