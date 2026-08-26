namespace ApiLinaAgbd.Models.Ventas.PedidosRecibidos
{
	public class PedidoSelectIdDto
	{
		public int id_pedido { get; set; }

		public int id_cliente { get; set; }

		public string cliente { get; set; } = string.Empty;

		public string telefono { get; set; } = string.Empty;

		public DateTime fecha_pedido { get; set; }

		public DateTime? fecha_entrega { get; set; }

		public string tipo_entrega { get; set; } = string.Empty;

		public decimal igv { get; set; }

		public string? ruta_comprobante { get; set; }

		public int estadoPedido { get; set; }

		public string estadoPedidoNombre { get; set; } = string.Empty;

		public int? id_pago { get; set; }

		public decimal? monto { get; set; }

		public string? metodoPago { get; set; }

		public string? codigo_operacion { get; set; }

		public List<PedidoDetalleDto> detalle { get; set; } = new();
	}
}