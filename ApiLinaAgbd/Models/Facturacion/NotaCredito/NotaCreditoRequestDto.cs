using System.ComponentModel.DataAnnotations;

namespace ApiLinaAgbd.Models.Facturacion.NotaCredito
{
	public class NotaCreditoEmitirRequestDto
	{
		[Required]
		public string VoucherReferenciaId { get; set; } = string.Empty;

		[Required]
		public string FechaEmision { get; set; } = string.Empty;

		public string? HoraEmision { get; set; }

		public string Moneda { get; set; } = "PEN";

		public decimal IgvPorcentaje { get; set; } = 18;

		public string? Observaciones { get; set; }

		[Required]
		public NotaCreditoMotivoDto Motivo { get; set; } = new();

		public List<NotaCreditoItemDto> Items { get; set; } = new();
	}

	public class NotaCreditoMotivoDto
	{
		[Required]
		public string Codigo { get; set; } = string.Empty;

		[Required]
		public string Descripcion { get; set; } = string.Empty;
	}

	public class NotaCreditoItemDto
	{
		public string? VoucherItemReferenciaId { get; set; }
		public int? ProductoId { get; set; }
		public string? Codigo { get; set; }

		public string Descripcion { get; set; } = string.Empty;

		public decimal Cantidad { get; set; }
		public decimal PrecioUnitario { get; set; }
		public string UnidadMedida { get; set; } = "NIU";
	}
}
